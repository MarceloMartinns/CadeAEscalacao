namespace CadeAEscalacao.Domain.Grupos;

/// <summary>
/// Quantos jogadores cada time tem em quadra e quantos reservas.
/// </summary>
public sealed record FormatoJogo
{
    public FormatoJogo(int jogadoresPorTime, int reservasPorTime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(jogadoresPorTime, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(reservasPorTime);

        JogadoresPorTime = jogadoresPorTime;
        ReservasPorTime = reservasPorTime;
    }

    public int JogadoresPorTime { get; }

    public int ReservasPorTime { get; }

    public int VagasPorTime => JogadoresPorTime + ReservasPorTime;

    /// <summary>Total de vagas na lista (dois times).</summary>
    public int TotalDeVagas => VagasPorTime * 2;

    /// <summary>Futebol society (sintético): 7 + 1 reserva por time.</summary>
    public static FormatoJogo Society { get; } = new(7, 1);

    /// <summary>Futsal (salão): 5 + 1 reserva por time.</summary>
    public static FormatoJogo Futsal { get; } = new(5, 1);
}
