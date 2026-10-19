using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;

namespace CadeAEscalacao.Application.Matches;

public sealed record MatchResponse(
    Guid Id,
    Guid GroupId,
    Guid LineupId,
    DateOnly Date,
    int GoalsA,
    int GoalsB,
    IReadOnlyList<Guid> TeamA,
    IReadOnlyList<Guid> TeamB)
{
    public static MatchResponse From(Match match) => new(
        match.Id.Value,
        match.GroupId.Value,
        match.LineupId.Value,
        match.Date,
        match.GoalsA,
        match.GoalsB,
        match.TeamA.Select(playerId => playerId.Value).ToList(),
        match.TeamB.Select(playerId => playerId.Value).ToList());
}

/// <summary>The coach of the week or the president records the score of a match played with a completed lineup.</summary>
public sealed class RecordMatch(
    ILineupRepository lineups,
    IWeeklyListRepository lists,
    IGroupRepository groups,
    IMatchRepository matches,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<MatchResponse> HandleAsync(
        LineupId lineupId, int goalsA, int goalsB, CancellationToken cancellationToken = default)
    {
        var lineup = await lineups.GetByIdAsync(lineupId, cancellationToken)
            ?? throw new NotFoundException("Lineup", lineupId);
        var group = await groups.GetByIdAsync(lineup.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", lineup.GroupId);
        Roles.EnsureCoachOrPresident(lineup, group, currentUser.PlayerId);
        if (await matches.ExistsForLineupAsync(lineupId, cancellationToken))
        {
            throw new ConflictException("The match of this lineup was already recorded.");
        }

        var list = await lists.GetByIdAsync(lineup.WeeklyListId, cancellationToken)
            ?? throw new NotFoundException("Weekly list", lineup.WeeklyListId);

        var match = Match.Record(list, lineup, goalsA, goalsB);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MatchResponse.From(match);
    }
}

public sealed class GetMatch(IMatchRepository matches, IGroupRepository groups, ICurrentUser currentUser)
{
    /// <summary>The match, or null if not found. Only members of the group can see it.</summary>
    public async Task<MatchResponse?> HandleAsync(MatchId id, CancellationToken cancellationToken = default)
    {
        if (await matches.GetByIdAsync(id, cancellationToken) is not { } match)
        {
            return null;
        }

        var group = await groups.GetByIdAsync(match.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", match.GroupId);
        Roles.EnsureMember(group, currentUser.PlayerId);
        return MatchResponse.From(match);
    }
}

/// <summary>The president fixes a score that was recorded wrong.</summary>
public sealed class CorrectScore(
    IMatchRepository matches, IGroupRepository groups, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<MatchResponse> HandleAsync(
        MatchId id, int goalsA, int goalsB, CancellationToken cancellationToken = default)
    {
        var match = await matches.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Match", id);
        var group = await groups.GetByIdAsync(match.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", match.GroupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);

        match.CorrectScore(goalsA, goalsB);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MatchResponse.From(match);
    }
}
