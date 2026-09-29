using CadeAEscalacao.Domain.Partidas;

namespace CadeAEscalacao.Domain.Tests;

public class PontuacaoTests
{
    [TestCase(Resultado.Vitoria, 3)]
    [TestCase(Resultado.Empate, 1)]
    [TestCase(Resultado.Derrota, 0)]
    public void Calcular_RetornaPontosDoResultado(Resultado resultado, int pontosEsperados)
    {
        Pontuacao.Calcular(resultado).ShouldBe(pontosEsperados);
    }

    [Test]
    public void Calcular_ResultadoInvalido_LancaExcecao()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Pontuacao.Calcular((Resultado)99));
    }
}
