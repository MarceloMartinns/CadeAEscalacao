using CadeAEscalacao.Domain.Grupos;

namespace CadeAEscalacao.Domain.Tests;

public class FormatoJogoTests
{
    [Test]
    public void Society_Tem7JogadoresE1ReservaPorTime()
    {
        var formato = FormatoJogo.Society;

        formato.JogadoresPorTime.ShouldBe(7);
        formato.ReservasPorTime.ShouldBe(1);
        formato.TotalDeVagas.ShouldBe(16);
    }

    [Test]
    public void Futsal_Tem5JogadoresE1ReservaPorTime()
    {
        var formato = FormatoJogo.Futsal;

        formato.JogadoresPorTime.ShouldBe(5);
        formato.ReservasPorTime.ShouldBe(1);
        formato.TotalDeVagas.ShouldBe(12);
    }

    [TestCase(0, 1)]
    [TestCase(5, -1)]
    public void Construtor_ValoresInvalidos_LancaExcecao(int jogadores, int reservas)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new FormatoJogo(jogadores, reservas));
    }
}
