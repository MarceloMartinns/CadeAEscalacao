using CadeAEscalacao.Domain.Groups;

namespace CadeAEscalacao.Domain.Seasons;

/// <summary>
/// Period in which points and attendance add up in the ranking of a group.
/// The duration is configurable (default: 6 months).
/// </summary>
public sealed class Season
{
    public const int DefaultDurationInMonths = 6;

    private Season(SeasonId id, GroupId groupId, string name, DateOnly start, DateOnly end)
    {
        Id = id;
        GroupId = groupId;
        Name = name;
        Start = start;
        End = end;
    }

    public SeasonId Id { get; }

    public GroupId GroupId { get; }

    public string Name { get; }

    public DateOnly Start { get; }

    /// <summary>Last day of the season (inclusive).</summary>
    public DateOnly End { get; }

    public static Season Create(
        GroupId groupId,
        string name,
        DateOnly start,
        int durationInMonths = DefaultDurationInMonths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(durationInMonths, 1);

        var end = start.AddMonths(durationInMonths).AddDays(-1);
        return new Season(SeasonId.New(), groupId, name.Trim(), start, end);
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;
}
