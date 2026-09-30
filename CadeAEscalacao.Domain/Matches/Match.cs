using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Domain.Matches;

/// <summary>
/// A match that was played: the teams (copied from the lineup) and the score. Aggregate root.
/// Everybody on a team gets the attendance and the points of the team's result.
/// </summary>
public sealed class Match
{
    private readonly List<PlayerId> _teamA = [];
    private readonly List<PlayerId> _teamB = [];

    private Match(MatchId id, GroupId groupId, LineupId lineupId, DateOnly date, int goalsA, int goalsB)
    {
        Id = id;
        GroupId = groupId;
        LineupId = lineupId;
        Date = date;
        GoalsA = goalsA;
        GoalsB = goalsB;
    }

    public MatchId Id { get; }

    public GroupId GroupId { get; }

    public LineupId LineupId { get; }

    public DateOnly Date { get; }

    public int GoalsA { get; private set; }

    public int GoalsB { get; private set; }

    public IReadOnlyList<PlayerId> TeamA => _teamA;

    public IReadOnlyList<PlayerId> TeamB => _teamB;

    public IEnumerable<PlayerId> Players => _teamA.Concat(_teamB);

    /// <summary>Records the score of a match played with a completed lineup.</summary>
    public static Match Record(WeeklyList list, Lineup lineup, int goalsA, int goalsB)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(lineup);
        EnsureValidScore(goalsA, goalsB);

        if (lineup.WeeklyListId != list.Id)
        {
            throw new DomainException("The lineup belongs to another list.");
        }

        if (list.Status == WeeklyListStatus.Cancelled)
        {
            throw new DomainException("The match was cancelled.");
        }

        if (lineup.Status != LineupStatus.Completed)
        {
            throw new DomainException("Finish the lineup before recording the match.");
        }

        var match = new Match(MatchId.New(), list.GroupId, lineup.Id, list.MatchDate, goalsA, goalsB);
        match._teamA.AddRange(lineup.TeamA);
        match._teamB.AddRange(lineup.TeamB);
        return match;
    }

    /// <summary>Fixes a score that was recorded wrong.</summary>
    public void CorrectScore(int goalsA, int goalsB)
    {
        EnsureValidScore(goalsA, goalsB);
        GoalsA = goalsA;
        GoalsB = goalsB;
    }

    public MatchResult ResultOf(Team team)
    {
        var (own, other) = team switch
        {
            Team.A => (GoalsA, GoalsB),
            Team.B => (GoalsB, GoalsA),
            _ => throw new ArgumentOutOfRangeException(nameof(team), team, "Invalid team."),
        };

        if (own > other)
        {
            return MatchResult.Win;
        }

        return own == other ? MatchResult.Draw : MatchResult.Loss;
    }

    /// <summary>The player's result, or null if the player did not play this match.</summary>
    public MatchResult? ResultOf(PlayerId playerId) => TeamOf(playerId) is { } team ? ResultOf(team) : null;

    /// <summary>The player's team, or null if the player did not play this match.</summary>
    public Team? TeamOf(PlayerId playerId)
    {
        if (_teamA.Contains(playerId))
        {
            return Team.A;
        }

        if (_teamB.Contains(playerId))
        {
            return Team.B;
        }

        return null;
    }

    private static void EnsureValidScore(int goalsA, int goalsB)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(goalsA);
        ArgumentOutOfRangeException.ThrowIfNegative(goalsB);
    }
}
