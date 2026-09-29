namespace CadeAEscalacao.Domain.Partidas;

/// <summary>
/// Regra de pontuação do ranking: vitória 3, empate 1, derrota 0.
/// </summary>
public static class Pontuacao
{
    public const int PontosPorVitoria = 3;
    public const int PontosPorEmpate = 1;
    public const int PontosPorDerrota = 0;

    public static int Calcular(Resultado resultado) => resultado switch
    {
        Resultado.Vitoria => PontosPorVitoria,
        Resultado.Empate => PontosPorEmpate,
        Resultado.Derrota => PontosPorDerrota,
        _ => throw new ArgumentOutOfRangeException(nameof(resultado), resultado, "Resultado inválido."),
    };
}
