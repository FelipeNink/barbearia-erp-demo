namespace Barbearia.Models
{
    /// <summary>
    /// Cargos fixos de antes dos cargos cadastrados. So a migracao usa,
    /// para converter o texto antigo de cargo_fun.
    /// </summary>
    public static class CargoFuncionario
    {
        public static readonly (string Valor, string Rotulo)[] Todos =
        {
            ("BARBEIRO", "Barbeiro"),
            ("RECEPCAO", "Recepção"),
            ("GERENTE", "Gerente"),
            ("AUXILIAR", "Auxiliar"),
            ("OUTRO", "Outro")
        };

        public static string Rotulo(string? valor) =>
            Todos.FirstOrDefault(c => c.Valor == valor).Rotulo ?? (valor ?? "—");
    }

    /// <summary>Como o profissional trabalha com a barbearia.</summary>
    public static class VinculoFuncionario
    {
        /// <summary>Parceiro da Lei 13.352/2016, com CNPJ proprio.</summary>
        public const string Mei = "MEI";

        public static readonly (string Valor, string Rotulo)[] Todos =
        {
            ("CLT", "CLT (carteira assinada)"),
            (Mei, "MEI (parceiro)"),
            ("AUTONOMO", "Autônomo"),
            ("ESTAGIO", "Estagiário"),
            ("SOCIO", "Sócio")
        };

        public static string Rotulo(string? valor) =>
            Todos.FirstOrDefault(v => v.Valor == valor).Rotulo ?? (valor ?? "—");
    }

    public class Funcionario : IEndereco
    {
        public int IdFuncionario { get; set; }
        public string? NomeFuncionario { get; set; }
        public string? Telefone { get; set; }
        public string? CPF { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string? CTPS { get; set; }
        public string? RG { get; set; }
        public string? Cep { get; set; }
        public string? Email { get; set; }
        public string? Estado { get; set; }
        public string? Cidade { get; set; }
        public string? Bairro { get; set; }
        public string? Rua { get; set; }
        public string? Numero { get; set; }

        /// <summary>Salário fixo mensal, somado às comissões na folha.</summary>
        public decimal SalarioFixo { get; set; }

        /// <summary>Cargo cadastrado em Configurações → Cargos (tabela funcao).</summary>
        public int? IdCargo { get; set; }

        /// <summary>Nome do cargo, so para exibir.</summary>
        public string? Cargo { get; set; }

        /// <summary>Não: o profissional não ganha comissão em serviço nenhum.</summary>
        public bool RecebeComissaoServico { get; set; } = true;

        /// <summary>% do profissional em serviços; vale no lugar da do serviço. Zero usa a do serviço.</summary>
        public decimal ComissaoServicoPadrao { get; set; }

        /// <summary>Não: o profissional não ganha comissão em produto nenhum.</summary>
        public bool RecebeComissaoProduto { get; set; } = true;

        /// <summary>% do profissional em produtos; vale no lugar da do produto. Zero usa a do produto.</summary>
        public decimal ComissaoProdutoPadrao { get; set; }

        public DateTime? DataAdmissao { get; set; }
        public DateTime? DataDesligamento { get; set; }
        public string? ChavePix { get; set; }
        public string? DadosBancarios { get; set; }

        /// <summary>Cor do profissional na agenda, em #rrggbb.</summary>
        public string? CorAgenda { get; set; }

        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Profissional-parceiro da Lei 13.352/2016, com CNPJ/MEI proprio.
        /// Por ora so cadastro: a NFS-e ainda sai inteira pelo salao.
        /// </summary>
        public bool Parceiro { get; set; }

        /// <summary>CLT, MEI etc. (<see cref="VinculoFuncionario"/>); MEI liga o <see cref="Parceiro"/>.</summary>
        public string? Vinculo { get; set; }
        public string? CnpjParceiro { get; set; }

        // Preenchidos na leitura
        public bool TemFoto { get; set; }

        /// <summary>Login ligado a este profissional (usuario.id_fun_fk), definido na tela Equipe.</summary>
        public string? LoginVinculado { get; set; }
    }

    /// <summary>Serviço que o profissional realiza; nulo nos valores é "usa o do serviço".</summary>
    public class FuncionarioServico
    {
        public int IdServico { get; set; }
        public decimal? Comissao { get; set; }
        public decimal? Preco { get; set; }
    }
}
