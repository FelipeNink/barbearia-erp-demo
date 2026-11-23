using Barbearia.Configs;
using Barbearia.Models;
using MySqlConnector;

namespace Barbearia.Data
{
    /// <summary>
    /// Folha de pagamento: salário fixo somado às comissões do mês.
    /// O pagamento só acontece aqui, e gera a despesa correspondente.
    /// </summary>
    public class FolhaDAO
    {
        private readonly Conexao _conexao;
        private readonly CaixaDAO _caixaDAO;

        public FolhaDAO(Conexao conexao, CaixaDAO caixaDAO)
        {
            _conexao = conexao;
            _caixaDAO = caixaDAO;
        }

        public class LinhaFolha
        {
            public int IdFuncionario { get; set; }
            public string NomeFuncionario { get; set; } = "";

            public decimal SalarioFixo { get; set; }
            public decimal Comissoes { get; set; }

            /// <summary>Parte do profissional nas gorjetas do mês (venda_gorjeta).</summary>
            public decimal Gorjetas { get; set; }

            /// <summary>Serviços executados no mês — produtos não contam.</summary>
            public int ServicosRealizados { get; set; }

            /// <summary>
            /// Produtos vendidos pelo profissional. Registrado mesmo quando
            /// não gera comissão, para acompanhar quem vende balcão.
            /// </summary>
            public int ProdutosVendidos { get; set; }
            public decimal ValorProdutos { get; set; }

            public decimal ValorServicos { get; set; }

            public int Atendimentos { get; set; }
            public decimal TotalVendido { get; set; }

            public bool Pago { get; set; }
            public DateTime? DataPagamento { get; set; }
            public decimal ValorPago { get; set; }

            public decimal TotalAReceber => SalarioFixo + Comissoes + Gorjetas;
        }

        /// <summary>Competência no formato AAAA-MM.</summary>
        public static string Competencia(DateTime mes) => mes.ToString("yyyy-MM");

        public List<LinhaFolha> Montar(DateTime mes)
        {
            var inicio = new DateTime(mes.Year, mes.Month, 1);
            var fim = inicio.AddMonths(1);
            var competencia = Competencia(inicio);

            var linhas = new List<LinhaFolha>();

            using var conexao = _conexao.GetConnection();

            // Todos os funcionarios entram, mesmo sem venda no mes: quem tem
            // salario fixo precisa aparecer para ser pago.
            using (var comando = new MySqlCommand(@"
                SELECT f.id_fun, f.nome_fun, f.salario_fixo
                FROM funcionario f
                ORDER BY f.nome_fun", conexao))
            using (var leitor = comando.ExecuteReader())
            {
                while (leitor.Read())
                {
                    linhas.Add(new LinhaFolha
                    {
                        IdFuncionario = leitor.GetInt32("id_fun"),
                        NomeFuncionario = leitor.IsDBNull(leitor.GetOrdinal("nome_fun"))
                            ? "(sem nome)" : leitor.GetString("nome_fun"),
                        SalarioFixo = leitor.GetDecimal("salario_fixo")
                    });
                }
            }

            // Fiado: a comissao e a gorjeta da parte a receber nao entram no
            // mes da venda (o fator tira essa fracao) e vao entrando no mes de
            // cada recebimento, na proporcao do que foi pago.
            const string FatorPago =
                "(1 - COALESCE(cr.valor / NULLIF(v.valor_vend + v.gorjeta_vend, 0), 0))";

            foreach (var linha in linhas)
            {
                using (var comando = new MySqlCommand($@"
                    SELECT
                      COALESCE(SUM(vi.comissao * vi.quantidade * {FatorPago}), 0) comissoes,
                      COALESCE(SUM(CASE WHEN vi.tipo_item <> 'PRODUTO' THEN vi.quantidade ELSE 0 END), 0) servicos,
                      COALESCE(SUM(CASE WHEN vi.tipo_item <> 'PRODUTO' THEN vi.valor_total ELSE 0 END), 0) valor_servicos,
                      COALESCE(SUM(CASE WHEN vi.tipo_item = 'PRODUTO' THEN vi.quantidade ELSE 0 END), 0) produtos,
                      COALESCE(SUM(CASE WHEN vi.tipo_item = 'PRODUTO' THEN vi.valor_total ELSE 0 END), 0) valor_produtos,
                      COALESCE(SUM(vi.valor_total), 0) vendido,
                      COUNT(DISTINCT v.id_vend) atendimentos
                    FROM venda v
                    JOIN venda_item vi ON vi.id_vend_fk = v.id_vend
                    LEFT JOIN conta_receber cr ON cr.id_vend_fk = v.id_vend
                    WHERE COALESCE(vi.id_fun_fk, v.id_fun_fk) = @id
                      AND v.data_vend >= @inicio AND v.data_vend < @fim
                      AND v.status_vend <> 'CANCELADA'", conexao))
                {
                    comando.Parameters.AddWithValue("@id", linha.IdFuncionario);
                    comando.Parameters.AddWithValue("@inicio", inicio);
                    comando.Parameters.AddWithValue("@fim", fim);

                    using var leitor = comando.ExecuteReader();
                    if (leitor.Read())
                    {
                        linha.Comissoes = Convert.ToDecimal(leitor.GetValue(leitor.GetOrdinal("comissoes")));
                        linha.ServicosRealizados = Convert.ToInt32(leitor.GetValue(leitor.GetOrdinal("servicos")));
                        linha.ValorServicos = leitor.GetDecimal("valor_servicos");
                        linha.ProdutosVendidos = Convert.ToInt32(leitor.GetValue(leitor.GetOrdinal("produtos")));
                        linha.ValorProdutos = leitor.GetDecimal("valor_produtos");
                        linha.TotalVendido = leitor.GetDecimal("vendido");
                        linha.Atendimentos = leitor.GetInt32("atendimentos");
                    }
                }

                using (var comando = new MySqlCommand($@"
                    SELECT COALESCE(SUM(g.valor * {FatorPago}), 0)
                    FROM venda_gorjeta g
                    JOIN venda v ON v.id_vend = g.id_vend_fk
                    LEFT JOIN conta_receber cr ON cr.id_vend_fk = v.id_vend
                    WHERE g.id_fun_fk = @id
                      AND v.data_vend >= @inicio AND v.data_vend < @fim
                      AND v.status_vend <> 'CANCELADA'", conexao))
                {
                    comando.Parameters.AddWithValue("@id", linha.IdFuncionario);
                    comando.Parameters.AddWithValue("@inicio", inicio);
                    comando.Parameters.AddWithValue("@fim", fim);
                    linha.Gorjetas = Convert.ToDecimal(comando.ExecuteScalar());
                }

                // A parte do fiado que o cliente pagou neste mes.
                using (var comando = new MySqlCommand(@"
                    SELECT
                      COALESCE((SELECT SUM(vi.comissao * vi.quantidade * r.valor / (v.valor_vend + v.gorjeta_vend))
                                  FROM conta_recebimento r
                                  JOIN conta_receber cr ON cr.id_conta = r.id_conta_fk
                                  JOIN venda v ON v.id_vend = cr.id_vend_fk
                                  JOIN venda_item vi ON vi.id_vend_fk = v.id_vend
                                 WHERE COALESCE(vi.id_fun_fk, v.id_fun_fk) = @id
                                   AND r.data_receb >= @inicio AND r.data_receb < @fim
                                   AND v.status_vend <> 'CANCELADA'), 0) comissoes,
                      COALESCE((SELECT SUM(g.valor * r.valor / (v.valor_vend + v.gorjeta_vend))
                                  FROM conta_recebimento r
                                  JOIN conta_receber cr ON cr.id_conta = r.id_conta_fk
                                  JOIN venda v ON v.id_vend = cr.id_vend_fk
                                  JOIN venda_gorjeta g ON g.id_vend_fk = v.id_vend
                                 WHERE g.id_fun_fk = @id
                                   AND r.data_receb >= @inicio AND r.data_receb < @fim
                                   AND v.status_vend <> 'CANCELADA'), 0) gorjetas", conexao))
                {
                    comando.Parameters.AddWithValue("@id", linha.IdFuncionario);
                    comando.Parameters.AddWithValue("@inicio", inicio);
                    comando.Parameters.AddWithValue("@fim", fim);

                    using var leitor = comando.ExecuteReader();
                    if (leitor.Read())
                    {
                        linha.Comissoes += Convert.ToDecimal(leitor.GetValue(0));
                        linha.Gorjetas += Convert.ToDecimal(leitor.GetValue(1));
                    }
                }

                linha.Comissoes = Math.Round(linha.Comissoes, 2);
                linha.Gorjetas = Math.Round(linha.Gorjetas, 2);

                using (var comando = new MySqlCommand(@"
                    SELECT data_pgto, total FROM folha_pagamento
                    WHERE id_fun_fk = @id AND competencia = @competencia", conexao))
                {
                    comando.Parameters.AddWithValue("@id", linha.IdFuncionario);
                    comando.Parameters.AddWithValue("@competencia", competencia);

                    using var leitor = comando.ExecuteReader();
                    if (leitor.Read())
                    {
                        linha.Pago = true;
                        linha.DataPagamento = leitor.GetDateTime("data_pgto");
                        linha.ValorPago = leitor.GetDecimal("total");
                    }
                }
            }

            return linhas;
        }

        /// <summary>
        /// Registra o pagamento e lança a despesa na categoria Salários,
        /// numa transação. A restrição de unicidade impede pagar duas vezes
        /// a mesma competência.
        /// </summary>
        public void Pagar(LinhaFolha linha, DateTime mes, string formaPagamento,
                          int? idCaixaAberto, string? observacao, string? usuario)
        {
            if (linha.TotalAReceber <= 0)
                throw new Exception("Não há valor a pagar para este profissional.");

            // Pagamento em dinheiro sai da gaveta, igual a sangria. Conferir
            // antes de abrir a transacao: barrar aqui devolve uma mensagem
            // clara em vez de deixar o caixa fechar com falta.
            if (formaPagamento == FormaPagamento.Dinheiro && idCaixaAberto > 0)
            {
                _caixaDAO.GarantirSaldoParaSaida(
                    idCaixaAberto!.Value, linha.TotalAReceber, "O pagamento");
            }

            var competencia = Competencia(mes);

            using var conexao = _conexao.GetConnection();
            using var transacao = conexao.BeginTransaction();
            try
            {
                var descricao = linha.Gorjetas > 0
                    ? $"Salário, comissões e gorjetas — {linha.NomeFuncionario} ({competencia})"
                    : $"Salário e comissões — {linha.NomeFuncionario} ({competencia})";

                using var cmdDespesa = new MySqlCommand(@"
                    INSERT INTO despesa
                        (data_desp, descricao, categoria, valor, forma_pagamento,
                         pago, id_caixa_fk, observacao,
                         data_vencimento, data_pagamento, id_fun_fk)
                    VALUES
                        (@data, @descricao, 'SALARIO', @valor, @forma,
                         1, @idCaixa, 'Gerada pela folha de pagamento',
                         @data, @data, @idFuncionario);
                    SELECT LAST_INSERT_ID();", conexao, transacao);

                cmdDespesa.Parameters.AddWithValue("@data", DateTime.Today);
                cmdDespesa.Parameters.AddWithValue("@idFuncionario", linha.IdFuncionario);
                cmdDespesa.Parameters.AddWithValue("@descricao", descricao);
                cmdDespesa.Parameters.AddWithValue("@valor", linha.TotalAReceber);
                cmdDespesa.Parameters.AddWithValue("@forma", formaPagamento);
                // Só amarra ao caixa se o dinheiro sair mesmo da gaveta.
                cmdDespesa.Parameters.AddWithValue("@idCaixa",
                    (formaPagamento == FormaPagamento.Dinheiro && idCaixaAberto > 0)
                        ? idCaixaAberto!.Value : (object)DBNull.Value);

                var idDespesa = Convert.ToInt32(cmdDespesa.ExecuteScalar());

                using var cmdFolha = new MySqlCommand(@"
                    INSERT INTO folha_pagamento
                        (id_fun_fk, competencia, salario_fixo, comissoes, gorjetas, total,
                         servicos, valor_servicos, produtos, valor_produtos,
                         data_pgto, id_desp_fk, observacao, usuario)
                    VALUES
                        (@id, @competencia, @salario, @comissoes, @gorjetas, @total,
                         @servicos, @valorServicos, @produtos, @valorProdutos,
                         @dataPgto, @idDespesa, @observacao, @usuario)",
                    conexao, transacao);

                cmdFolha.Parameters.AddWithValue("@valorServicos", linha.ValorServicos);
                cmdFolha.Parameters.AddWithValue("@produtos", linha.ProdutosVendidos);
                cmdFolha.Parameters.AddWithValue("@valorProdutos", linha.ValorProdutos);

                cmdFolha.Parameters.AddWithValue("@id", linha.IdFuncionario);
                cmdFolha.Parameters.AddWithValue("@competencia", competencia);
                cmdFolha.Parameters.AddWithValue("@salario", linha.SalarioFixo);
                cmdFolha.Parameters.AddWithValue("@comissoes", linha.Comissoes);
                cmdFolha.Parameters.AddWithValue("@gorjetas", linha.Gorjetas);
                cmdFolha.Parameters.AddWithValue("@total", linha.TotalAReceber);
                cmdFolha.Parameters.AddWithValue("@servicos", linha.ServicosRealizados);
                cmdFolha.Parameters.AddWithValue("@dataPgto", DateTime.Now);
                cmdFolha.Parameters.AddWithValue("@idDespesa", idDespesa);
                cmdFolha.Parameters.AddWithValue("@observacao",
                    string.IsNullOrWhiteSpace(observacao) ? (object)DBNull.Value : observacao.Trim());
                cmdFolha.Parameters.AddWithValue("@usuario", (object?)usuario ?? DBNull.Value);

                cmdFolha.ExecuteNonQuery();
                transacao.Commit();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                transacao.Rollback();
                throw new Exception(
                    $"{linha.NomeFuncionario} já foi pago na competência {competencia}.");
            }
            catch (Exception ex)
            {
                transacao.Rollback();
                throw new Exception("Erro ao registrar o pagamento: " + ex.Message);
            }
        }

        /// <summary>Um pagamento de folha ja registrado.</summary>
        public record PagamentoFolha(
            DateTime Data, int IdFuncionario, string Profissional, string Competencia,
            decimal SalarioFixo, decimal Comissoes, decimal Total,
            string? FormaPagamento, string? Observacao, string? Usuario);

        /// <summary>Pagamentos mais recentes primeiro, com a forma vinda da despesa gerada.</summary>
        public List<PagamentoFolha> Historico(int limite = 240)
        {
            var lista = new List<PagamentoFolha>();

            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(@"
                SELECT p.data_pgto, p.id_fun_fk, f.nome_fun, p.competencia,
                       p.salario_fixo, p.comissoes, p.total, p.observacao, p.usuario,
                       d.forma_pagamento
                FROM folha_pagamento p
                JOIN funcionario f ON f.id_fun = p.id_fun_fk
                LEFT JOIN despesa d ON d.id_desp = p.id_desp_fk
                ORDER BY p.competencia DESC, p.data_pgto DESC
                LIMIT @limite", conexao);

            comando.Parameters.AddWithValue("@limite", limite);

            using var leitor = comando.ExecuteReader();
            var iNome = leitor.GetOrdinal("nome_fun");
            var iForma = leitor.GetOrdinal("forma_pagamento");
            var iObs = leitor.GetOrdinal("observacao");
            var iUsuario = leitor.GetOrdinal("usuario");

            while (leitor.Read())
            {
                lista.Add(new PagamentoFolha(
                    leitor.GetDateTime("data_pgto"),
                    leitor.GetInt32("id_fun_fk"),
                    leitor.IsDBNull(iNome) ? "(sem nome)" : leitor.GetString(iNome),
                    leitor.GetString("competencia"),
                    leitor.GetDecimal("salario_fixo"),
                    leitor.GetDecimal("comissoes"),
                    leitor.GetDecimal("total"),
                    leitor.IsDBNull(iForma) ? null : leitor.GetString(iForma),
                    leitor.IsDBNull(iObs) ? null : leitor.GetString(iObs),
                    leitor.IsDBNull(iUsuario) ? null : leitor.GetString(iUsuario)));
            }

            return lista;
        }
    }
}
