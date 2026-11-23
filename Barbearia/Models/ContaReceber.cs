namespace Barbearia.Models
{
    /// <summary>
    /// O fiado de uma venda: uma por venda, abatida pelos recebimentos.
    /// A comissao e a gorjeta da parte fiado so vao para a folha conforme
    /// o cliente paga (ver FolhaDAO).
    /// </summary>
    public class ContaReceber
    {
        public int IdConta { get; set; }
        public int IdVenda { get; set; }
        public int IdCliente { get; set; }
        public decimal Valor { get; set; }
        public decimal Recebido { get; set; }
        public DateTime? Vencimento { get; set; }
        public DateTime DataVenda { get; set; }

        // Preenchidos por join
        public string? NomeCliente { get; set; }
        public string? TelefoneCliente { get; set; }
        public string? Descricao { get; set; }

        public decimal Saldo => Valor - Recebido;
        public bool Atrasada => Saldo > 0 && Vencimento.HasValue && Vencimento.Value.Date < DateTime.Today;
    }

    /// <summary>Um pagamento do cliente contra o fiado.</summary>
    public class RecebimentoConta
    {
        public int IdRecebimento { get; set; }
        public int IdConta { get; set; }
        public int IdVenda { get; set; }
        public DateTime Data { get; set; }
        public decimal Valor { get; set; }
        public string Forma { get; set; } = FormaPagamento.Dinheiro;
        public int? IdCaixa { get; set; }
        public string? Usuario { get; set; }

        // Preenchidos por join
        public string? NomeCliente { get; set; }
        /// <summary>O caixa do recebimento ainda esta aberto: da para estornar.</summary>
        public bool CaixaAberto { get; set; }
    }
}
