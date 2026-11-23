using Barbearia.Configs;
using Barbearia.Models;
using MySqlConnector;

namespace Barbearia.Data
{
    /// <summary>
    /// Fiado: o que os clientes devem e o que pagaram. A conta nasce na
    /// venda (VendaDAO); aqui so se recebe e se estorna.
    /// </summary>
    public class ContaReceberDAO
    {
        private readonly Conexao _conexao;

        public ContaReceberDAO(Conexao conexao)
        {
            _conexao = conexao;
        }

        private const string Recebido = @"
            (SELECT COALESCE(SUM(r.valor), 0) FROM conta_recebimento r WHERE r.id_conta_fk = c.id_conta)";

        /// <summary>Contas com saldo, da mais antiga para a mais nova.</summary>
        public List<ContaReceber> ListarAbertas(int? idCliente = null)
        {
            using var conexao = _conexao.GetConnection();
            return ListarAbertas(conexao, null, idCliente);
        }

        private static List<ContaReceber> ListarAbertas(
            MySqlConnection conexao, MySqlTransaction? transacao, int? idCliente)
        {
            var lista = new List<ContaReceber>();

            using var comando = new MySqlCommand($@"
                SELECT c.id_conta, c.id_vend_fk, c.id_cli_fk, c.valor, c.vencimento,
                       v.data_vend, v.descricao_vend, cl.nome_cli, cl.telefone_cli,
                       {Recebido} AS recebido
                  FROM conta_receber c
                  JOIN venda v ON v.id_vend = c.id_vend_fk
                  JOIN cliente cl ON cl.id_cli = c.id_cli_fk
                 WHERE v.status_vend <> 'CANCELADA'
                   {(idCliente.HasValue ? "AND c.id_cli_fk = @idCliente" : "")}
                HAVING c.valor > recebido
                 ORDER BY v.data_vend, c.id_conta"
                + (transacao != null ? " FOR UPDATE" : ""), conexao, transacao);

            if (idCliente.HasValue) comando.Parameters.AddWithValue("@idCliente", idCliente.Value);

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                lista.Add(new ContaReceber
                {
                    IdConta = leitor.GetInt32("id_conta"),
                    IdVenda = leitor.GetInt32("id_vend_fk"),
                    IdCliente = leitor.GetInt32("id_cli_fk"),
                    Valor = leitor.GetDecimal("valor"),
                    Recebido = Convert.ToDecimal(leitor.GetValue(leitor.GetOrdinal("recebido"))),
                    Vencimento = DAOHelper.GetDateTime(leitor, "vencimento"),
                    DataVenda = leitor.GetDateTime("data_vend"),
                    Descricao = DAOHelper.GetString(leitor, "descricao_vend"),
                    NomeCliente = DAOHelper.GetString(leitor, "nome_cli"),
                    TelefoneCliente = DAOHelper.GetString(leitor, "telefone_cli")
                });
            }

            return lista;
        }

        /// <summary>Quanto o cliente deve hoje, somando todas as contas.</summary>
        public decimal SaldoDoCliente(int idCliente) =>
            ListarAbertas(idCliente).Sum(c => c.Saldo);

        /// <summary>
        /// Registra o pagamento do cliente, abatendo das contas mais antigas
        /// primeiro. Precisa do caixa aberto: o dinheiro entra nele.
        /// </summary>
        public void Receber(int idCliente, decimal valor, string forma, int idCaixa, string? usuario)
        {
            valor = Math.Round(valor, 2);
            if (valor <= 0) throw new Exception("Informe o valor recebido.");
            if (!FormaPagamento.Todas.Any(f => f.Valor == forma)) throw new Exception("Forma de pagamento inválida.");

            using var conexao = _conexao.GetConnection();
            using var transacao = conexao.BeginTransaction();
            try
            {
                using (var caixa = new MySqlCommand(
                    "SELECT status FROM caixa WHERE id_caixa = @id FOR UPDATE", conexao, transacao))
                {
                    caixa.Parameters.AddWithValue("@id", idCaixa);
                    if (caixa.ExecuteScalar() as string != StatusCaixa.Aberto)
                        throw new Exception("Abra o caixa para receber.");
                }

                var contas = ListarAbertas(conexao, transacao, idCliente);
                var saldo = contas.Sum(c => c.Saldo);
                if (valor > saldo)
                    throw new Exception($"O cliente deve {saldo:C}; não dá para receber {valor:C}.");

                var agora = DateTime.Now;
                var falta = valor;
                foreach (var conta in contas)
                {
                    if (falta <= 0) break;
                    var parte = Math.Min(falta, conta.Saldo);

                    using var comando = new MySqlCommand(@"
                        INSERT INTO conta_recebimento (id_conta_fk, data_receb, valor, forma, id_caixa_fk, usuario)
                        VALUES (@conta, @data, @valor, @forma, @caixa, @usuario)", conexao, transacao);
                    comando.Parameters.AddWithValue("@conta", conta.IdConta);
                    comando.Parameters.AddWithValue("@data", agora);
                    comando.Parameters.AddWithValue("@valor", parte);
                    comando.Parameters.AddWithValue("@forma", forma);
                    comando.Parameters.AddWithValue("@caixa", idCaixa);
                    comando.Parameters.AddWithValue("@usuario", (object?)usuario ?? DBNull.Value);
                    comando.ExecuteNonQuery();

                    falta -= parte;
                }

                transacao.Commit();
            }
            catch (Exception ex)
            {
                transacao.Rollback();
                throw new Exception("Erro ao receber: " + ex.Message);
            }
        }

        /// <summary>Recebimentos do período, mais recentes primeiro.</summary>
        public List<RecebimentoConta> Recebimentos(DateTime de, DateTime ate)
        {
            var lista = new List<RecebimentoConta>();

            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(@"
                SELECT r.id_receb, r.id_conta_fk, c.id_vend_fk, r.data_receb, r.valor, r.forma,
                       r.id_caixa_fk, r.usuario, cl.nome_cli, cx.status AS status_caixa
                  FROM conta_recebimento r
                  JOIN conta_receber c ON c.id_conta = r.id_conta_fk
                  JOIN cliente cl ON cl.id_cli = c.id_cli_fk
                  LEFT JOIN caixa cx ON cx.id_caixa = r.id_caixa_fk
                 WHERE r.data_receb >= @de AND r.data_receb < @ate
                 ORDER BY r.data_receb DESC, r.id_receb DESC", conexao);
            comando.Parameters.AddWithValue("@de", de.Date);
            comando.Parameters.AddWithValue("@ate", ate.Date.AddDays(1));

            using var leitor = comando.ExecuteReader();
            var iCaixa = leitor.GetOrdinal("id_caixa_fk");
            while (leitor.Read())
            {
                lista.Add(new RecebimentoConta
                {
                    IdRecebimento = leitor.GetInt32("id_receb"),
                    IdConta = leitor.GetInt32("id_conta_fk"),
                    IdVenda = leitor.GetInt32("id_vend_fk"),
                    Data = leitor.GetDateTime("data_receb"),
                    Valor = leitor.GetDecimal("valor"),
                    Forma = leitor.GetString("forma"),
                    IdCaixa = leitor.IsDBNull(iCaixa) ? null : leitor.GetInt32(iCaixa),
                    Usuario = DAOHelper.GetString(leitor, "usuario"),
                    NomeCliente = DAOHelper.GetString(leitor, "nome_cli"),
                    CaixaAberto = DAOHelper.GetString(leitor, "status_caixa") == StatusCaixa.Aberto
                });
            }

            return lista;
        }

        /// <summary>
        /// Desfaz um recebimento lancado errado. So enquanto o caixa dele
        /// estiver aberto: depois de conferido, o dinheiro ja foi contado.
        /// </summary>
        public void Estornar(int idRecebimento)
        {
            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(@"
                DELETE r FROM conta_recebimento r
                  JOIN caixa cx ON cx.id_caixa = r.id_caixa_fk
                 WHERE r.id_receb = @id AND cx.status = @aberto", conexao);
            comando.Parameters.AddWithValue("@id", idRecebimento);
            comando.Parameters.AddWithValue("@aberto", StatusCaixa.Aberto);

            if (comando.ExecuteNonQuery() == 0)
                throw new Exception("Só dá para estornar recebimento de caixa ainda aberto.");
        }
    }
}
