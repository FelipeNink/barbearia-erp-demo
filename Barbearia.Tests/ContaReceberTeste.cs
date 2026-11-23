using Barbearia.Data;
using Barbearia.Models;
using MySqlConnector;
using Xunit;

namespace Barbearia.Tests
{
    /// <summary>
    /// Fiado: a venda a receber vira conta do cliente, o recebimento abate
    /// das mais antigas, e a comissao so chega a folha conforme o cliente paga.
    /// </summary>
    public class ContaReceberTeste
    {
        [FatoComBanco]
        public void Comissao_do_fiado_entra_na_folha_conforme_o_cliente_paga()
        {
            using var conexao = BancoDeTeste.Abrir();
            var idFuncionario = Criar(conexao, "INSERT INTO funcionario (nome_fun, salario_fixo) VALUES (@nome, 0)");
            var idCliente = Criar(conexao, "INSERT INTO cliente (nome_cli) VALUES (@nome)");
            var idCaixa = Criar(conexao,
                "INSERT INTO caixa (data_abertura, valor_abertura, status) VALUES (NOW(), 0, 'ABERTO')");
            var vendasCriadas = new List<int>();

            try
            {
                var fabrica = BancoDeTeste.Fabricar();
                var vendas = new VendaDAO(fabrica);
                var contas = new ContaReceberDAO(fabrica);
                var folha = new FolhaDAO(fabrica, new CaixaDAO(fabrica));

                Venda Fiado(decimal valor, int? cliente) => new()
                {
                    DataVenda = DateTime.Now,
                    IdFuncionario = idFuncionario,
                    IdCliente = cliente,
                    IdCaixa = idCaixa,
                    Usuario = "Teste",
                    Pagamentos = { new VendaPagamento { Forma = FormaPagamento.AReceber, Valor = valor } },
                    Itens = { new VendaItem { TipoItem = TipoItem.Servico, Descricao = "Corte", Quantidade = 1, ValorUnitario = valor, ComissaoUnitaria = valor * 0.4m } }
                };

                decimal Comissao() => folha.Montar(DateTime.Today).Single(l => l.IdFuncionario == idFuncionario).Comissoes;

                // Sem cliente nao ha de quem cobrar.
                Assert.Throws<Exception>(() => vendas.Inserir(Fiado(100m, null)));

                var primeira = vendas.Inserir(Fiado(100m, idCliente));
                vendasCriadas.Add(primeira);
                var segunda = vendas.Inserir(Fiado(50m, idCliente));
                vendasCriadas.Add(segunda);

                Assert.Equal(150m, contas.SaldoDoCliente(idCliente));
                Assert.Equal(0m, Comissao());

                // 120: quita a primeira (100) e abate 20 da segunda.
                contas.Receber(idCliente, 120m, FormaPagamento.Dinheiro, idCaixa, "Teste");

                var abertas = contas.ListarAbertas(idCliente);
                Assert.Equal(segunda, Assert.Single(abertas).IdVenda);
                Assert.Equal(30m, contas.SaldoDoCliente(idCliente));
                Assert.Equal(48m, Comissao()); // 40 da primeira + 8 de 20/50 da segunda

                // Nao recebe mais do que deve, e venda paga em parte nao cancela.
                Assert.Throws<Exception>(() => contas.Receber(idCliente, 31m, FormaPagamento.Pix, idCaixa, "Teste"));
                Assert.Throws<Exception>(() => vendas.Cancelar(primeira, "Teste", "Teste"));

                // O dinheiro do fiado entra na gaveta.
                var caixa = new CaixaDAO(fabrica).BuscarAberto()!;
                Assert.Equal(120m, caixa.Resumo.RecebimentosFiadoDinheiro);

                // Estorno devolve o saldo enquanto o caixa esta aberto.
                var recebimento = contas.Recebimentos(DateTime.Today, DateTime.Today).First(r => r.IdVenda == segunda);
                contas.Estornar(recebimento.IdRecebimento);
                Assert.Equal(50m, contas.SaldoDoCliente(idCliente));
                Assert.Equal(40m, Comissao());
            }
            finally
            {
                foreach (var id in vendasCriadas) Executar(conexao, "DELETE FROM venda WHERE id_vend = @id", id);
                Executar(conexao, "DELETE FROM caixa WHERE id_caixa = @id", idCaixa);
                Executar(conexao, "DELETE FROM cliente WHERE id_cli = @id", idCliente);
                Executar(conexao, "DELETE FROM funcionario WHERE id_fun = @id", idFuncionario);
            }
        }

        private static int Criar(MySqlConnection conexao, string insert)
        {
            using var comando = new MySqlCommand(insert + "; SELECT LAST_INSERT_ID();", conexao);
            comando.Parameters.AddWithValue("@nome", BancoDeTeste.NomeUnico("Fiado"));
            return Convert.ToInt32(comando.ExecuteScalar());
        }

        private static void Executar(MySqlConnection conexao, string sql, int id)
        {
            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@id", id);
            comando.ExecuteNonQuery();
        }
    }
}
