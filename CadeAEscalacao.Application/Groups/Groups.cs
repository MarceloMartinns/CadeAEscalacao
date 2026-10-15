using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Groups;

// Minimal group registration (members, pairs and coach rotation), so the API can be used
// before the screens (etapa 8). Who may do each action (the president) comes in etapa 7.

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

public sealed class CreateGroup(IGroupRepository groups, IPlayerRepository players, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        string name,
        MatchFormat format,
        DayOfWeek matchDay,
        PlayerId presidentId,
        CancellationToken cancellationToken = default)
    {
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

public sealed class GetGroup(IGroupRepository groups)
{
    /// <summary>The group, or null if not found.</summary>
    public async Task<GroupResponse?> HandleAsync(GroupId id, CancellationToken cancellationToken = default) =>
        await groups.GetByIdAsync(id, cancellationToken) is { } group ? GroupResponse.From(group) : null;
}

public sealed class AddGroupMember(IGroupRepository groups, IPlayerRepository players, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, PlayerId playerId, PlayerType type, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        if (!await players.ExistsAsync(playerId, cancellationToken))
        {
            throw new NotFoundException("Player", playerId);
        }

        group.AddMember(playerId, type);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GroupResponse.From(group);
    }
}

public sealed class CreatePair(IGroupRepository groups, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, PlayerId first, PlayerId second, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);

        group.CreatePair(first, second);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GroupResponse.From(group);
    }
}

public sealed class ReorderCoachRotation(IGroupRepository groups, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, IReadOnlyList<PlayerId> order, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);

        group.ReorderCoachRotation(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GroupResponse.From(group);
    }
}
