using Barbearia.Data;
using Barbearia.Models;
using MySqlConnector;
using Xunit;

namespace Barbearia.Tests
{
    /// <summary>
    /// Gorjeta: paga junto com a venda, fora do faturamento e da comissao,
    /// dividida igual entre os profissionais dos itens e somada na folha.
    /// </summary>
    public class GorjetaTeste
    {
        [Fact]
        public void Divide_igual_e_a_sobra_fica_com_o_primeiro()
        {
            var partes = Venda.DividirGorjeta(10m, new[] { 1, 2, 3 });

            Assert.Equal(new[] { 3.34m, 3.33m, 3.33m }, partes.Select(p => p.Valor));
            Assert.Equal(10m, partes.Sum(p => p.Valor));
        }

        [Fact]
        public void Mesmo_profissional_em_varios_itens_conta_uma_vez()
        {
            var partes = Venda.DividirGorjeta(5m, new[] { 7, 7, 0 });

            Assert.Single(partes);
            Assert.Equal((7, 5m), partes[0]);
        }

        [Fact]
        public void Sem_gorjeta_nao_divide_nada()
        {
            Assert.Empty(Venda.DividirGorjeta(0m, new[] { 1, 2 }));
        }

        [FatoComBanco]
        public void Gorjeta_vai_para_a_folha_e_nao_para_a_comissao()
        {
            using var conexao = BancoDeTeste.Abrir();
            var corte = CriarFuncionario(conexao);
            var barba = CriarFuncionario(conexao);
            int? idVenda = null;

            try
            {
                var fabrica = BancoDeTeste.Fabricar();
                var vendas = new VendaDAO(fabrica);
                var folha = new FolhaDAO(fabrica, new CaixaDAO(fabrica));

                Venda NovaVenda(decimal pago) => new()
                {
                    DataVenda = DateTime.Now,
                    IdFuncionario = corte,
                    Gorjeta = 10m,
                    Usuario = "Teste",
                    Pagamentos = { new VendaPagamento { Forma = FormaPagamento.Pix, Valor = pago } },
                    Itens =
                    {
                        new VendaItem { TipoItem = TipoItem.Servico, Descricao = "Corte", Quantidade = 1, ValorUnitario = 30m, ComissaoUnitaria = 12m },
                        new VendaItem { TipoItem = TipoItem.Servico, Descricao = "Barba", Quantidade = 1, ValorUnitario = 20m, ComissaoUnitaria = 8m, IdFuncionario = barba }
                    }
                };

                // Pagamento so da venda, sem a gorjeta, nao fecha.
                Assert.Throws<Exception>(() => vendas.Inserir(NovaVenda(50m)));

                idVenda = vendas.Inserir(NovaVenda(60m));

                var gravada = vendas.BuscarPorId(idVenda.Value)!;
                Assert.Equal(50m, gravada.ValorVenda);
                Assert.Equal(10m, gravada.Gorjeta);

                var linhas = folha.Montar(DateTime.Today);
                var linhaCorte = linhas.Single(l => l.IdFuncionario == corte);
                var linhaBarba = linhas.Single(l => l.IdFuncionario == barba);

                Assert.Equal(5m, linhaCorte.Gorjetas);
                Assert.Equal(5m, linhaBarba.Gorjetas);
                Assert.Equal(12m, linhaCorte.Comissoes);
                Assert.Equal(17m, linhaCorte.TotalAReceber);

                // Cancelada, a gorjeta sai da folha junto.
                vendas.Cancelar(idVenda.Value, "Teste", "Teste");
                Assert.Equal(0m, folha.Montar(DateTime.Today).Single(l => l.IdFuncionario == corte).Gorjetas);
            }
            finally
            {
                if (idVenda.HasValue) ApagarVenda(conexao, idVenda.Value);
                ApagarFuncionario(conexao, corte);
                ApagarFuncionario(conexao, barba);
            }
        }

        private static int CriarFuncionario(MySqlConnection conexao)
        {
            using var comando = new MySqlCommand(@"
                INSERT INTO funcionario (nome_fun, salario_fixo)
                VALUES (@nome, 0);
                SELECT LAST_INSERT_ID();", conexao);

            comando.Parameters.AddWithValue("@nome", BancoDeTeste.NomeUnico("Profissional"));
            return Convert.ToInt32(comando.ExecuteScalar());
        }

        private static void ApagarFuncionario(MySqlConnection conexao, int id)
        {
            using var comando = new MySqlCommand(
                "DELETE FROM funcionario WHERE id_fun = @id", conexao);

            comando.Parameters.AddWithValue("@id", id);
            comando.ExecuteNonQuery();
        }

        private static void ApagarVenda(MySqlConnection conexao, int id)
        {
            using var comando = new MySqlCommand("DELETE FROM venda WHERE id_vend = @id", conexao);
            comando.Parameters.AddWithValue("@id", id);
            comando.ExecuteNonQuery();
        }
    }
}
