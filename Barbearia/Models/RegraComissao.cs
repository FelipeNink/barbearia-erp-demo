namespace Barbearia.Models
{
    /// <summary>
    /// Comissão por unidade, em R$, do jeito que fica congelada no item da venda.
    ///
    /// Profissional que não recebe comissão (de serviço ou de produto) ganha
    /// zero. Senão, serviço: a comissão própria do profissional para o
    /// serviço vale primeiro; depois o % do profissional; sem ele (zero), a
    /// do serviço. Produto: o % do profissional ou, sem ele, o do produto.
    ///
    /// O % do profissional vem antes do serviço porque é a experiência que
    /// muda a comissão: o iniciante ganha menos no mesmo corte.
    /// </summary>
    public static class RegraComissao
    {
        public static decimal Servico(Servico servico, Funcionario? profissional,
                                      FuncionarioServico? doProfissional, decimal baseUnitaria)
        {
            if (profissional is { RecebeComissaoServico: false }) return 0;

            var tipo = servico.TipoComissao;
            decimal valor;

            if (doProfissional?.Comissao is decimal propria) valor = propria;
            else if (profissional is { ComissaoServicoPadrao: > 0 } p)
            {
                tipo = TipoComissao.Percentual;
                valor = p.ComissaoServicoPadrao;
            }
            else valor = (decimal)servico.ComissaoServico;

            return tipo == TipoComissao.Percentual
                ? Math.Round(baseUnitaria * valor / 100m, 2)
                : Math.Round(valor, 2);
        }

        public static decimal Produto(Produto produto, Funcionario? profissional, decimal baseUnitaria)
        {
            if (profissional is { RecebeComissaoProduto: false }) return 0;

            var percentual = profissional is { ComissaoProdutoPadrao: > 0 } p
                ? p.ComissaoProdutoPadrao
                : produto.Comissao ?? 0;

            return Math.Round(baseUnitaria * percentual / 100m, 2);
        }
    }
}
