using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Groups;

// Minimal group registration (members, pairs and coach rotation), so the API can be used
// before the screens (etapa 8). Only the president changes the group; members can see it (ADR-016).

public sealed record GroupResponse(
    Guid Id,
    string Name,
    int PlayersPerTeam,
    int BenchPlayersPerTeam,
    DayOfWeek MatchDay,
    Guid PresidentId,
    IReadOnlyList<GroupMemberResponse> Members,
    IReadOnlyList<PairResponse> Pairs,
    IReadOnlyList<Guid> CoachRotation)
{
    public static GroupResponse From(Group group) => new(
        group.Id.Value,
        group.Name,
        group.Format.PlayersPerTeam,
        group.Format.BenchPlayersPerTeam,
        group.MatchDay,
        group.PresidentId.Value,
        group.Members.Select(member => new GroupMemberResponse(member.PlayerId.Value, member.Type)).ToList(),
        group.Pairs.Select(pair => new PairResponse(pair.First.Value, pair.Second.Value)).ToList(),
        group.CoachRotation.Select(playerId => playerId.Value).ToList());
}

public sealed record GroupMemberResponse(Guid PlayerId, PlayerType Type);

public sealed record PairResponse(Guid FirstPlayerId, Guid SecondPlayerId);

/// <summary>Creates a group. Whoever creates it becomes the president (ADR-017).</summary>
public sealed class CreateGroup(
    IGroupRepository groups, IPlayerRepository players, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        string name, MatchFormat format, DayOfWeek matchDay, CancellationToken cancellationToken = default)
    {
        var presidentId = currentUser.PlayerId;
        if (!await players.ExistsAsync(presidentId, cancellationToken))
        {
            throw new NotFoundException("Player", presidentId);
        }

        var group = Group.Create(name, format, matchDay, presidentId);
        groups.Add(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GroupResponse.From(group);
    }
}

public sealed class GetGroup(IGroupRepository groups, ICurrentUser currentUser)
{
    /// <summary>The group, or null if not found. Only members can see it.</summary>
    public async Task<GroupResponse?> HandleAsync(GroupId id, CancellationToken cancellationToken = default)
    {
        if (await groups.GetByIdAsync(id, cancellationToken) is not { } group)
        {
            return null;
        }

        Roles.EnsureMember(group, currentUser.PlayerId);
        return GroupResponse.From(group);
    }
}

public sealed class AddGroupMember(
    IGroupRepository groups, IPlayerRepository players, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, PlayerId playerId, PlayerType type, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);
        if (!await players.ExistsAsync(playerId, cancellationToken))
        {
            throw new NotFoundException("Player", playerId);
        }

        group.AddMember(playerId, type);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GroupResponse.From(group);
    }
}

public sealed class CreatePair(IGroupRepository groups, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, PlayerId first, PlayerId second, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);

        group.CreatePair(first, second);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GroupResponse.From(group);
    }
}

public sealed class ReorderCoachRotation(IGroupRepository groups, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, IReadOnlyList<PlayerId> order, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);

        group.ReorderCoachRotation(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GroupResponse.From(group);
    }
}
