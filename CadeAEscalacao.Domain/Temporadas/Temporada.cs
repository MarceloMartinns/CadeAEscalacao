namespace CadeAEscalacao.Domain.Temporadas;

/// <summary>
/// Período em que pontos e presenças são somados no ranking.
/// A duração é configurável (padrão: 6 meses).
/// </summary>
public sealed class Temporada
{
    public const int DuracaoPadraoEmMeses = 6;

    private Temporada(string nome, DateOnly inicio, DateOnly fim)
    {
        Nome = nome;
        Inicio = inicio;
        Fim = fim;
    }

    public string Nome { get; }

    public DateOnly Inicio { get; }

    /// <summary>Último dia da temporada (inclusivo).</summary>
    public DateOnly Fim { get; }

    public static Temporada Criar(string nome, DateOnly inicio, int duracaoEmMeses = DuracaoPadraoEmMeses)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentOutOfRangeException.ThrowIfLessThan(duracaoEmMeses, 1);

        var fim = inicio.AddMonths(duracaoEmMeses).AddDays(-1);
        return new Temporada(nome.Trim(), inicio, fim);
    }

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;
}
