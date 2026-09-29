using CadeAEscalacao.Domain.Temporadas;

namespace CadeAEscalacao.Domain.Tests;

public class TemporadaTests
{
    [Test]
    public void Criar_SemDuracao_Usa6Meses()
    {
        var temporada = Temporada.Criar("2026/2", new DateOnly(2026, 7, 1));

        temporada.Fim.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Test]
    public void Criar_ComDuracaoCustomizada_CalculaFim()
    {
        var temporada = Temporada.Criar("Curta", new DateOnly(2026, 1, 1), duracaoEmMeses: 3);

        temporada.Fim.ShouldBe(new DateOnly(2026, 3, 31));
    }

    [TestCase(2026, 7, 1, true)]
    [TestCase(2026, 12, 31, true)]
    [TestCase(2026, 6, 30, false)]
    [TestCase(2027, 1, 1, false)]
    public void Contem_VerificaLimitesInclusivos(int ano, int mes, int dia, bool esperado)
    {
        var temporada = Temporada.Criar("2026/2", new DateOnly(2026, 7, 1));

        temporada.Contem(new DateOnly(ano, mes, dia)).ShouldBe(esperado);
    }

    [Test]
    public void Criar_DuracaoZero_LancaExcecao()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => Temporada.Criar("Inválida", new DateOnly(2026, 1, 1), duracaoEmMeses: 0));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Criar_NomeVazio_LancaExcecao(string nome)
    {
        Should.Throw<ArgumentException>(() => Temporada.Criar(nome, new DateOnly(2026, 1, 1)));
    }
}
