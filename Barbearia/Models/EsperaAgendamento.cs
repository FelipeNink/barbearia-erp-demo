namespace Barbearia.Models
{
    public static class StatusEspera
    {
        public const string Aguardando = "AGUARDANDO";
        /// <summary>A equipe mandou a vaga pelo WhatsApp; segue na lista ate agendar ou desistir.</summary>
        public const string Avisado = "AVISADO";
        public const string Agendado = "AGENDADO";
        public const string Desistiu = "DESISTIU";

        /// <summary>Os que ainda aparecem na lista.</summary>
        public static readonly string[] Ativos = { Aguardando, Avisado };
    }

    /// <summary>Parte do dia que o cliente aceita.</summary>
    public static class PeriodoEspera
    {
        public const string Qualquer = "QUALQUER";
        public const string Manha = "MANHA";
        public const string Tarde = "TARDE";
        public const string Noite = "NOITE";

        public static readonly (string Valor, string Rotulo)[] Todos =
        {
            (Qualquer, "Qualquer horário"),
            (Manha, "Manhã (até 12h)"),
            (Tarde, "Tarde (12h às 18h)"),
            (Noite, "Noite (depois das 18h)")
        };

        public static string Rotulo(string? valor) =>
            Todos.FirstOrDefault(p => p.Valor == valor).Rotulo ?? "Qualquer horário";

        public static bool Aceita(string? periodo, DateTime horario) => periodo switch
        {
            Manha => horario.Hour < 12,
            Tarde => horario.Hour >= 12 && horario.Hour < 18,
            Noite => horario.Hour >= 18,
            _ => true
        };
    }

    /// <summary>
    /// Cliente que queria um horario que nao tinha. Quando abre vaga no dia
    /// e periodo dele, a lista mostra o horario para a equipe avisar.
    /// </summary>
    public class EsperaAgendamento
    {
        public int IdEspera { get; set; }
        public int IdCliente { get; set; }
        public int? IdServico { get; set; }
        /// <summary>Nulo: aceita qualquer profissional.</summary>
        public int? IdFuncionario { get; set; }
        public DateTime Data { get; set; } = DateTime.Today;
        public string Periodo { get; set; } = PeriodoEspera.Qualquer;
        public string? Observacao { get; set; }
        public string Status { get; set; } = StatusEspera.Aguardando;
        public DateTime CriadoEm { get; set; }
        public DateTime? AvisadoEm { get; set; }

        // Preenchidos por join
        public string? NomeCliente { get; set; }
        public string? TelefoneCliente { get; set; }
        public string? NomeServico { get; set; }
        public int DuracaoServico { get; set; }
        public string? NomeFuncionario { get; set; }

        // Preenchidos por EsperaDAO.ProcurarVagas
        public DateTime? Vaga { get; set; }
        public int? VagaIdFuncionario { get; set; }
        public string? VagaNomeFuncionario { get; set; }

        public bool Avisado => Status == StatusEspera.Avisado;

        /// <summary>
        /// O primeiro horario livre que serve: no dia e periodo pedidos, com
        /// o profissional preferido ou, sem preferencia, com qualquer um dos
        /// <paramref name="profissionais"/>. Nunca antes de <paramref name="agora"/>.
        /// </summary>
        public static (DateTime Horario, int IdFuncionario)? PrimeiraVaga(
            QuadroAgenda quadro, EsperaAgendamento espera, IEnumerable<int> profissionais,
            int intervaloMin, DateTime agora)
        {
            var duracao = espera.DuracaoServico > 0 ? espera.DuracaoServico : 30;
            var candidatos = espera.IdFuncionario is > 0 ? new[] { espera.IdFuncionario.Value } : profissionais;

            return candidatos
                .SelectMany(id => quadro.Livres(id, espera.Data, duracao, intervaloMin, agora)
                    .Where(h => PeriodoEspera.Aceita(espera.Periodo, h))
                    .Take(1)
                    .Select(h => (Horario: h, IdFuncionario: id)))
                .OrderBy(v => v.Horario)
                .Select(v => ((DateTime, int)?)v)
                .FirstOrDefault();
        }
    }
}
