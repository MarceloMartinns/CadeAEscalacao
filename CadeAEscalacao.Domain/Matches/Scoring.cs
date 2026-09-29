namespace CadeAEscalacao.Domain.Matches;

/// <summary>
/// Ranking points: win 3, draw 1, loss 0.
/// </summary>
public static class Scoring
{
    public const int PointsPerWin = 3;
    public const int PointsPerDraw = 1;
    public const int PointsPerLoss = 0;

    public static int Calculate(MatchResult result) => result switch
    {
        MatchResult.Win => PointsPerWin,
        MatchResult.Draw => PointsPerDraw,
        MatchResult.Loss => PointsPerLoss,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Invalid match result."),
    };
}
