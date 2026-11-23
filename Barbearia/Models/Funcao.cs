namespace Barbearia.Models
{
    /// <summary>Cargo da equipe. Ligado ao acesso e ao funcionario por id, nunca por texto.</summary>
    public class Funcao
    {
        public int IdFuncao { get; set; }
        public string Nome { get; set; } = "";

        /// <summary>Quantas pessoas (acessos e funcionarios) estao neste cargo. Zero permite excluir.</summary>
        public int Membros { get; set; }

        public bool EmUso => Membros > 0;
    }
}
