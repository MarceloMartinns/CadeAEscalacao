namespace CadeAEscalacao.Domain.Groups;

/// <summary>
/// How many players each team has on the field and how many on the bench.
/// </summary>
public sealed record MatchFormat
{
    public MatchFormat(int playersPerTeam, int benchPlayersPerTeam)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(playersPerTeam, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(benchPlayersPerTeam);

        PlayersPerTeam = playersPerTeam;
        BenchPlayersPerTeam = benchPlayersPerTeam;
    }

    /// <summary>
    /// Used by EF Core only (complex type): it creates the object and then sets the properties.
    /// </summary>
    private MatchFormat()
    {
    }

    public int PlayersPerTeam { get; private set; }

    public int BenchPlayersPerTeam { get; private set; }

    public int SpotsPerTeam => PlayersPerTeam + BenchPlayersPerTeam;

    /// <summary>Total spots on the weekly list (two teams).</summary>
    public int TotalSpots => SpotsPerTeam * 2;

    /// <summary>Seven-a-side on artificial turf ("society"): 7 + 1 bench player per team.</summary>
    public static MatchFormat SevenASide { get; } = new(7, 1);

    /// <summary>Futsal (indoor court): 5 + 1 bench player per team.</summary>
    public static MatchFormat Futsal { get; } = new(5, 1);
}
