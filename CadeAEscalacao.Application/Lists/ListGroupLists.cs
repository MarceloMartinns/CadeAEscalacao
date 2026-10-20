using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Lists;

/// <summary>
/// A list of the group, with its lineup and match when they exist: where the group screen
/// starts (ADR-021).
/// </summary>
public sealed record WeeklyListSummaryResponse(
    Guid Id,
    DateOnly MatchDate,
    WeeklyListStatus Status,
    Guid? LineupId,
    LineupStatus? LineupStatus,
    Guid? MatchId);

public sealed class ListGroupLists(
    IWeeklyListRepository lists,
    ILineupRepository lineups,
    IMatchRepository matches,
    IGroupRepository groups,
    ICurrentUser currentUser)
{
    /// <summary>How many lists the screen shows: about five months of weekly matches.</summary>
    public const int MaxLists = 20;

    /// <summary>The most recent lists first. Only members can see them.</summary>
    public async Task<IReadOnlyList<WeeklyListSummaryResponse>> HandleAsync(
        GroupId groupId, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        Roles.EnsureMember(group, currentUser.PlayerId);

        var recentLists = await lists.ListRecentByGroupAsync(groupId, MaxLists, cancellationToken);
        var lineupsByList = (await lineups.ListByWeeklyListsAsync(
                recentLists.Select(list => list.Id).ToList(), cancellationToken))
            .ToDictionary(lineup => lineup.WeeklyListId);
        var matchesByLineup = (await matches.ListByLineupsAsync(
                lineupsByList.Values.Select(lineup => lineup.Id).ToList(), cancellationToken))
            .ToDictionary(match => match.LineupId);

        return recentLists
            .OrderByDescending(list => list.MatchDate)
            .Select(list =>
            {
                var lineup = lineupsByList.GetValueOrDefault(list.Id);
                var match = lineup is null ? null : matchesByLineup.GetValueOrDefault(lineup.Id);
                return new WeeklyListSummaryResponse(
                    list.Id.Value, list.MatchDate, list.Status, lineup?.Id.Value, lineup?.Status, match?.Id.Value);
            })
            .ToList();
    }
}
