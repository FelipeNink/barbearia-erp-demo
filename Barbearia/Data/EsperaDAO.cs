using Barbearia.Configs;
using Barbearia.Models;
using MySqlConnector;

namespace Barbearia.Data
{
    /// <summary>
    /// Lista de espera da agenda. Aberta na tela de Agendamentos; a vaga de
    /// cada cliente sai do mesmo quadro que o agendamento online usa.
    /// </summary>
    public class EsperaDAO
    {
        private readonly Conexao _conexao;

        public EsperaDAO(Conexao conexao)
        {
            _conexao = conexao;
        }

        public int Inserir(EsperaAgendamento espera)
        {
            if (espera.IdCliente <= 0) throw new Exception("Selecione o cliente.");
            if (espera.Data.Date < DateTime.Today) throw new Exception("Escolha hoje ou um dia futuro.");

            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(@"
                INSERT INTO espera_agendamento
                    (id_cli_fk, id_serv_fk, id_fun_fk, data_desejada, periodo, observacao)
                VALUES (@cliente, @servico, @funcionario, @data, @periodo, @obs);
                SELECT LAST_INSERT_ID();", conexao);
            comando.Parameters.AddWithValue("@cliente", espera.IdCliente);
            comando.Parameters.AddWithValue("@servico", espera.IdServico is > 0 ? espera.IdServico.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@funcionario", espera.IdFuncionario is > 0 ? espera.IdFuncionario.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@data", espera.Data.Date);
            comando.Parameters.AddWithValue("@periodo", espera.Periodo);
            comando.Parameters.AddWithValue("@obs",
                string.IsNullOrWhiteSpace(espera.Observacao) ? DBNull.Value : espera.Observacao.Trim());
            return Convert.ToInt32(comando.ExecuteScalar());
        }

        /// <summary>Quem ainda espera, de hoje em diante, pelo dia pedido.</summary>
        public List<EsperaAgendamento> ListarAtivas()
        {
            var lista = new List<EsperaAgendamento>();

            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(@"
                SELECT e.*, c.nome_cli, c.telefone_cli, s.nome_serv, s.duracao_min, f.nome_fun
                  FROM espera_agendamento e
                  JOIN cliente c ON c.id_cli = e.id_cli_fk
                  LEFT JOIN servico s ON s.id_serv = e.id_serv_fk
                  LEFT JOIN funcionario f ON f.id_fun = e.id_fun_fk
                 WHERE e.status IN (@aguardando, @avisado) AND e.data_desejada >= @hoje
                 ORDER BY e.data_desejada, e.criado_em", conexao);
            comando.Parameters.AddWithValue("@aguardando", StatusEspera.Aguardando);
            comando.Parameters.AddWithValue("@avisado", StatusEspera.Avisado);
            comando.Parameters.AddWithValue("@hoje", DateTime.Today);

            using var leitor = comando.ExecuteReader();
            var iServ = leitor.GetOrdinal("id_serv_fk");
            var iFun = leitor.GetOrdinal("id_fun_fk");
            var iDuracao = leitor.GetOrdinal("duracao_min");
            while (leitor.Read())
            {
                lista.Add(new EsperaAgendamento
                {
                    IdEspera = leitor.GetInt32("id_espera"),
                    IdCliente = leitor.GetInt32("id_cli_fk"),
                    IdServico = leitor.IsDBNull(iServ) ? null : leitor.GetInt32(iServ),
                    IdFuncionario = leitor.IsDBNull(iFun) ? null : leitor.GetInt32(iFun),
                    Data = leitor.GetDateTime("data_desejada"),
                    Periodo = leitor.GetString("periodo"),
                    Observacao = DAOHelper.GetString(leitor, "observacao"),
                    Status = leitor.GetString("status"),
                    CriadoEm = leitor.GetDateTime("criado_em"),
                    AvisadoEm = DAOHelper.GetDateTime(leitor, "avisado_em"),
                    NomeCliente = DAOHelper.GetString(leitor, "nome_cli"),
                    TelefoneCliente = DAOHelper.GetString(leitor, "telefone_cli"),
                    NomeServico = DAOHelper.GetString(leitor, "nome_serv"),
                    DuracaoServico = leitor.IsDBNull(iDuracao) ? 0 : leitor.GetInt32(iDuracao),
                    NomeFuncionario = DAOHelper.GetString(leitor, "nome_fun")
                });
            }

            return lista;
        }

        /// <summary>
        /// Preenche a primeira vaga de cada um. Sem profissional preferido,
        /// vale qualquer ativo que faca o servico (ou todos os ativos, se
        /// ninguem tiver o servico marcado no cadastro).
        /// </summary>
        public void ProcurarVagas(List<EsperaAgendamento> lista, DateTime agora)
        {
            if (lista.Count == 0) return;

            var funcionarioDAO = new FuncionarioDAO(_conexao);
            var ativos = funcionarioDAO.ListarTodos().Where(f => f.Ativo).ToList();
            var mapa = funcionarioDAO.MapaServicos();
            var nomes = ativos.ToDictionary(f => f.IdFuncionario, f => f.NomeFuncionario);

            var profissionais = ativos.Select(f => f.IdFuncionario)
                .Concat(lista.Where(e => e.IdFuncionario is > 0).Select(e => e.IdFuncionario!.Value))
                .Distinct().ToList();

            var de = lista.Min(e => e.Data).Date;
            var ate = lista.Max(e => e.Data).Date.AddDays(1);
            var agendaOnline = new AgendaOnlineDAO(_conexao);
            var quadro = agendaOnline.CarregarQuadro(profissionais, de, ate);
            var intervalo = agendaOnline.LerConfig().IntervaloMin;

            foreach (var espera in lista)
            {
                var quem = ativos.Select(f => f.IdFuncionario).ToList();
                if (espera.IdServico is > 0)
                {
                    var fazem = quem.Where(id => mapa.ContainsKey((id, espera.IdServico.Value))).ToList();
                    if (fazem.Count > 0) quem = fazem;
                }

                var vaga = EsperaAgendamento.PrimeiraVaga(quadro, espera, quem, intervalo, agora);
                espera.Vaga = vaga?.Horario;
                espera.VagaIdFuncionario = vaga?.IdFuncionario;
                espera.VagaNomeFuncionario = vaga.HasValue && nomes.TryGetValue(vaga.Value.IdFuncionario, out var nome)
                    ? nome : null;
            }
        }

        public void MarcarAvisado(int idEspera) =>
            MudarStatus(idEspera, StatusEspera.Avisado, "avisado_em = NOW()");

        public void Desistir(int idEspera) => MudarStatus(idEspera, StatusEspera.Desistiu, null);

        /// <summary>Saiu da lista porque o horario foi marcado.</summary>
        public void MarcarAgendado(int idEspera, int idAgendamento)
        {
            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(@"
                UPDATE espera_agendamento SET status = @status, id_agend_fk = @agendamento
                 WHERE id_espera = @id", conexao);
            comando.Parameters.AddWithValue("@status", StatusEspera.Agendado);
            comando.Parameters.AddWithValue("@agendamento", idAgendamento);
            comando.Parameters.AddWithValue("@id", idEspera);
            comando.ExecuteNonQuery();
        }

        private void MudarStatus(int idEspera, string status, string? extra)
        {
            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand($@"
                UPDATE espera_agendamento SET status = @status{(extra == null ? "" : ", " + extra)}
                 WHERE id_espera = @id", conexao);
            comando.Parameters.AddWithValue("@status", status);
            comando.Parameters.AddWithValue("@id", idEspera);
            comando.ExecuteNonQuery();
        }
    }
}
