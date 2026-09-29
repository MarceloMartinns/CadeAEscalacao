namespace CadeAEscalacao.Domain.Seasons;

/// <summary>
/// Period in which points and attendance add up in the ranking.
/// The duration is configurable (default: 6 months).
/// </summary>
public sealed class Season
{
    public const int DefaultDurationInMonths = 6;

    private Season(string name, DateOnly start, DateOnly end)
    {
        Name = name;
        Start = start;
        End = end;
    }

    public string Name { get; }

    public DateOnly Start { get; }

    /// <summary>Last day of the season (inclusive).</summary>
    public DateOnly End { get; }

    public static Season Create(string name, DateOnly start, int durationInMonths = DefaultDurationInMonths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(durationInMonths, 1);

        var end = start.AddMonths(durationInMonths).AddDays(-1);
        return new Season(name.Trim(), start, end);
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;
}
