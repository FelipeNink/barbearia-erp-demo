using Barbearia.Models;
using Xunit;

namespace Barbearia.Tests
{
    /// <summary>A vaga que a lista de espera oferece: dia, periodo e profissional pedidos.</summary>
    public class ListaEsperaTeste
    {
        private static readonly DateTime Segunda = new(2026, 10, 19);
        private static readonly DateTime Antes = new(2026, 1, 1);
        private const int Luis = 1, Beto = 2;

        /// <summary>Barbearia 9h-19h; Luis ocupado o dia todo menos 15h-15h30, Beto livre so 17h-17h30.</summary>
        private static QuadroAgenda Quadro() => new()
        {
            Grade = Enum.GetValues<DayOfWeek>()
                .Select(d => new HorarioDia { Dia = d, Aberto = true, Abre = new(9, 0, 0), Fecha = new(19, 0, 0) })
                .ToList(),
            Excecoes = new Dictionary<DateTime, bool>(),
            Jornadas = new Dictionary<int, List<JornadaDia>?> { [Luis] = null, [Beto] = null },
            Ocupados = new Dictionary<int, List<Periodo>>
            {
                [Luis] = new() { new(Segunda.AddHours(9), Segunda.AddHours(15)), new(Segunda.AddHours(15.5), Segunda.AddHours(19)) },
                [Beto] = new() { new(Segunda.AddHours(9), Segunda.AddHours(17)), new(Segunda.AddHours(17.5), Segunda.AddHours(19)) }
            }
        };

        private static EsperaAgendamento Espera(string periodo, int? profissional = null) =>
            new() { Data = Segunda, Periodo = periodo, IdFuncionario = profissional, DuracaoServico = 30 };

        [Fact]
        public void Sem_preferencia_pega_o_primeiro_horario_de_qualquer_profissional()
        {
            var vaga = EsperaAgendamento.PrimeiraVaga(Quadro(), Espera(PeriodoEspera.Qualquer), new[] { Luis, Beto }, 30, Antes);

            Assert.Equal((Segunda.AddHours(15), Luis), vaga);
        }

        [Fact]
        public void Respeita_o_profissional_preferido()
        {
            var vaga = EsperaAgendamento.PrimeiraVaga(Quadro(), Espera(PeriodoEspera.Qualquer, Beto), new[] { Luis, Beto }, 30, Antes);

            Assert.Equal((Segunda.AddHours(17), Beto), vaga);
        }

        [Fact]
        public void Fora_do_periodo_pedido_nao_e_vaga()
        {
            Assert.Null(EsperaAgendamento.PrimeiraVaga(Quadro(), Espera(PeriodoEspera.Manha), new[] { Luis, Beto }, 30, Antes));
            Assert.Null(EsperaAgendamento.PrimeiraVaga(Quadro(), Espera(PeriodoEspera.Noite), new[] { Luis, Beto }, 30, Antes));
        }

        [Fact]
        public void Horario_que_ja_passou_nao_conta()
        {
            var vaga = EsperaAgendamento.PrimeiraVaga(Quadro(), Espera(PeriodoEspera.Tarde), new[] { Luis, Beto }, 30,
                Segunda.AddHours(16));

            Assert.Equal((Segunda.AddHours(17), Beto), vaga);
        }
    }
}
