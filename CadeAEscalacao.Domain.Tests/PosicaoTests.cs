using CadeAEscalacao.Domain.Jogadores;

namespace CadeAEscalacao.Domain.Tests;

public class PosicaoTests
{
    [Test]
    public void JogadorMeioAtaque_JogaNasDuasPosicoes()
    {
        var posicoes = Posicao.Meio | Posicao.Ataque;

        posicoes.HasFlag(Posicao.Meio).ShouldBeTrue();
        posicoes.HasFlag(Posicao.Ataque).ShouldBeTrue();
        posicoes.HasFlag(Posicao.Goleiro).ShouldBeFalse();
    }
}
