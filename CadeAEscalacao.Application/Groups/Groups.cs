using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Groups;

// Minimal group registration (members, pairs and coach rotation). Only the president changes
// the group; members can see it (ADR-016).

/// <summary>
/// The group with the name of each member. The screens use it to show names for the player IDs
/// of lists, lineups, matches and rankings (ADR-021).
/// </summary>
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
    internal static async Task<GroupResponse> CreateAsync(
        Group group, IPlayerRepository players, CancellationToken cancellationToken)
    {
        var names = await players.GetNamesAsync(
            group.Members.Select(member => member.PlayerId).ToList(), cancellationToken);

        return new GroupResponse(
            group.Id.Value,
            group.Name,
            group.Format.PlayersPerTeam,
            group.Format.BenchPlayersPerTeam,
            group.MatchDay,
            group.PresidentId.Value,
            group.Members
                .Select(member => new GroupMemberResponse(
                    member.PlayerId.Value, names.GetValueOrDefault(member.PlayerId, string.Empty), member.Type))
                .ToList(),
            group.Pairs.Select(pair => new PairResponse(pair.First.Value, pair.Second.Value)).ToList(),
            group.CoachRotation.Select(playerId => playerId.Value).ToList());
    }
}

public sealed record GroupMemberResponse(Guid PlayerId, string Name, PlayerType Type);

public sealed record PairResponse(Guid FirstPlayerId, Guid SecondPlayerId);

/// <summary>A group in the list of the signed-in player's groups.</summary>
public sealed record GroupSummaryResponse(
    Guid Id, string Name, DayOfWeek MatchDay, int PlayersPerTeam, int BenchPlayersPerTeam, Guid PresidentId)
{
    public static GroupSummaryResponse From(Group group) => new(
        group.Id.Value,
        group.Name,
        group.MatchDay,
        group.Format.PlayersPerTeam,
        group.Format.BenchPlayersPerTeam,
        group.PresidentId.Value);
}

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
        return await GroupResponse.CreateAsync(group, players, cancellationToken);
    }
}

/// <summary>The groups of the signed-in player, by name (ADR-021).</summary>
public sealed class ListMyGroups(IGroupRepository groups, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<GroupSummaryResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var myGroups = await groups.ListByMemberAsync(currentUser.PlayerId, cancellationToken);
        return myGroups
            .OrderBy(group => group.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(GroupSummaryResponse.From)
            .ToList();
    }
}

public sealed class GetGroup(IGroupRepository groups, IPlayerRepository players, ICurrentUser currentUser)
{
    /// <summary>The group, or null if not found. Only members can see it.</summary>
    public async Task<GroupResponse?> HandleAsync(GroupId id, CancellationToken cancellationToken = default)
    {
        if (await groups.GetByIdAsync(id, cancellationToken) is not { } group)
        {
            return null;
        }

        Roles.EnsureMember(group, currentUser.PlayerId);
        return await GroupResponse.CreateAsync(group, players, cancellationToken);
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
        return await GroupResponse.CreateAsync(group, players, cancellationToken);
    }
}

/// <summary>
/// The president adds, by e-mail, someone who already created their profile (ADR-021).
/// The president is checked first, so only the president learns whether the e-mail has an account.
/// </summary>
public sealed class AddGroupMemberByEmail(
    IGroupRepository groups,
    IPlayerRepository players,
    IAccountDirectory accounts,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, string email, PlayerType type, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);

        var playerId = await accounts.FindPlayerIdByEmailAsync(email, cancellationToken)
            ?? throw new NotFoundException("Player with e-mail", email);

        group.AddMember(playerId, type);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GroupResponse.CreateAsync(group, players, cancellationToken);
    }
}

public sealed class CreatePair(
    IGroupRepository groups, IPlayerRepository players, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, PlayerId first, PlayerId second, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);

        group.CreatePair(first, second);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GroupResponse.CreateAsync(group, players, cancellationToken);
    }
}

public sealed class ReorderCoachRotation(
    IGroupRepository groups, IPlayerRepository players, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public async Task<GroupResponse> HandleAsync(
        GroupId groupId, IReadOnlyList<PlayerId> order, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);

        group.ReorderCoachRotation(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GroupResponse.CreateAsync(group, players, cancellationToken);
    }
}
