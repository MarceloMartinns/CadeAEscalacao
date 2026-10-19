using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Lineups;

public sealed class GetLineup(ILineupRepository lineups, IGroupRepository groups, ICurrentUser currentUser)
{
    /// <summary>The lineup, or null if not found. Only members of the group can see it.</summary>
    public async Task<LineupResponse?> HandleAsync(LineupId id, CancellationToken cancellationToken = default)
    {
        if (await lineups.GetByIdAsync(id, cancellationToken) is not { } lineup)
        {
            return null;
        }

        var group = await groups.GetByIdAsync(lineup.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", lineup.GroupId);
        Roles.EnsureMember(group, currentUser.PlayerId);
        return LineupResponse.From(lineup);
    }
}

/// <summary>The coach's partner is not playing: the president picks one of the players.</summary>
public sealed class SetPartnerOfTheDay(
    ILineupRepository lineups, IGroupRepository groups, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<LineupResponse> HandleAsync(
        LineupId id, PlayerId partnerId, CancellationToken cancellationToken = default)
    {
        var lineup = await lineups.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Lineup", id);
        var group = await groups.GetByIdAsync(lineup.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", lineup.GroupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);

        lineup.SetPartnerOfTheDay(partnerId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return LineupResponse.From(lineup);
    }
}

/// <summary>
/// The coach of the week splits the players (without themselves and their partner) into two teams.
/// Only the coach can do it ("só o professor da vez publica a escalação", ADR-016).
/// </summary>
public sealed class AssignTeams(ILineupRepository lineups, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<LineupResponse> HandleAsync(
        LineupId id,
        IReadOnlyCollection<PlayerId> teamA,
        IReadOnlyCollection<PlayerId> teamB,
        CancellationToken cancellationToken = default)
    {
        var lineup = await lineups.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Lineup", id);
        Roles.EnsureCoach(lineup, currentUser.PlayerId);

        lineup.AssignTeams(teamA, teamB);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return LineupResponse.From(lineup);
    }
}

/// <summary>
/// The partner picks a team and the coach goes to the other one. The lineup is final and
/// the coach goes to the end of the group's rotation; both aggregates are saved together.
/// </summary>
public sealed class ChoosePartnerTeam(
    ILineupRepository lineups, IGroupRepository groups, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<LineupResponse> HandleAsync(LineupId id, Team team, CancellationToken cancellationToken = default)
    {
        var lineup = await lineups.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Lineup", id);
        Roles.EnsurePartner(lineup, currentUser.PlayerId);
        var group = await groups.GetByIdAsync(lineup.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", lineup.GroupId);

        lineup.PartnerChooses(team);
        group.CompleteCoachTurn(lineup.CoachId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return LineupResponse.From(lineup);
    }
}
