using Barbearia.Models;
using Xunit;

namespace Barbearia.Tests
{
    /// <summary>
    /// Ordem da comissao: nao recebe → zero; comissao propria no servico;
    /// % do profissional; comissao do servico/produto. E o que faz o
    /// iniciante ganhar menos no mesmo corte.
    /// </summary>
    public class RegraComissaoTeste
    {
        // Corte de R$ 50 com comissao fixa de R$ 20 no cadastro do servico.
        private static Servico Corte() => new()
        {
            ComissaoServico = 20,
            TipoComissao = TipoComissao.Fixo
        };

        [Fact]
        public void Sem_percentual_do_profissional_vale_a_do_servico()
            => Assert.Equal(20m, RegraComissao.Servico(Corte(), new Funcionario(), null, 50m));

        [Fact]
        public void Percentual_do_profissional_vale_no_lugar_do_servico()
        {
            var iniciante = new Funcionario { ComissaoServicoPadrao = 30 };
            Assert.Equal(15m, RegraComissao.Servico(Corte(), iniciante, null, 50m));
        }

        [Fact]
        public void Comissao_propria_no_servico_vence_o_percentual_do_profissional()
        {
            var profissional = new Funcionario { ComissaoServicoPadrao = 30 };
            var propria = new FuncionarioServico { Comissao = 25 };
            Assert.Equal(25m, RegraComissao.Servico(Corte(), profissional, propria, 50m));
        }

        [Fact]
        public void Quem_nao_recebe_comissao_de_servico_ganha_zero()
        {
            var profissional = new Funcionario { RecebeComissaoServico = false, ComissaoServicoPadrao = 30 };
            var propria = new FuncionarioServico { Comissao = 25 };
            Assert.Equal(0m, RegraComissao.Servico(Corte(), profissional, propria, 50m));
        }

        [Fact]
        public void Produto_usa_o_percentual_do_profissional_e_sem_ele_o_do_produto()
        {
            var pomada = new Produto { Comissao = 10 };
            Assert.Equal(4m, RegraComissao.Produto(pomada, new Funcionario(), 40m));
            Assert.Equal(8m, RegraComissao.Produto(pomada, new Funcionario { ComissaoProdutoPadrao = 20 }, 40m));
            Assert.Equal(0m, RegraComissao.Produto(pomada, new Funcionario { RecebeComissaoProduto = false }, 40m));
        }
    }
}
