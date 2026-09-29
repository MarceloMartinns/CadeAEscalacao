using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Groups;

/// <summary>
/// A weekly game slot ("horário"): format, match day, members, president and pairs.
/// Aggregate root: members and pairs only change through its methods,
/// so the rules below always hold.
/// </summary>
/// <remarks>
/// Rules:
/// <list type="bullet">
/// <item>The president is always a regular member.</item>
/// <item>A pair has two different members, and a player is in at most one pair.</item>
/// <item>Removing a member dissolves their pair.</item>
/// </list>
/// </remarks>
public sealed class Group
{
    private readonly List<GroupMember> _members = [];
    private readonly List<Pair> _pairs = [];

    private Group(GroupId id, string name, MatchFormat format, DayOfWeek matchDay, PlayerId presidentId)
    {
        Id = id;
        Name = name;
        Format = format;
        MatchDay = matchDay;
        PresidentId = presidentId;
    }

    public GroupId Id { get; }

    public string Name { get; private set; }

    public MatchFormat Format { get; private set; }

    public DayOfWeek MatchDay { get; private set; }

    public PlayerId PresidentId { get; private set; }

    public IReadOnlyList<GroupMember> Members => _members;

    public IReadOnlyList<Pair> Pairs => _pairs;

    /// <summary>Creates the group with the president as its first regular member.</summary>
    public static Group Create(string name, MatchFormat format, DayOfWeek matchDay, PlayerId presidentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(format);

        var group = new Group(GroupId.New(), name.Trim(), format, matchDay, presidentId);
        group._members.Add(new GroupMember(presidentId, PlayerType.Regular));
        return group;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void ChangeFormat(MatchFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        Format = format;
    }

    public bool IsMember(PlayerId playerId) => FindMember(playerId) is not null;

    public GroupMember GetMember(PlayerId playerId) =>
        FindMember(playerId) ?? throw new DomainException("The player is not a member of this group.");

    public void AddMember(PlayerId playerId, PlayerType type)
    {
        if (IsMember(playerId))
        {
            throw new DomainException("The player is already a member of this group.");
        }

        _members.Add(new GroupMember(playerId, type));
    }

    public void ChangeMemberType(PlayerId playerId, PlayerType type)
    {
        var member = GetMember(playerId);

        if (playerId == PresidentId && type != PlayerType.Regular)
        {
            throw new DomainException("The president must be a regular player.");
        }

        _members[_members.IndexOf(member)] = member with { Type = type };
    }

    public void RemoveMember(PlayerId playerId)
    {
        if (playerId == PresidentId)
        {
            throw new DomainException("The president cannot leave the group. Transfer the presidency first.");
        }

        var member = GetMember(playerId);
        _pairs.RemoveAll(pair => pair.Contains(playerId));
        _members.Remove(member);
    }

    public void TransferPresidency(PlayerId newPresidentId)
    {
        if (!GetMember(newPresidentId).IsRegular)
        {
            throw new DomainException("The president must be a regular player.");
        }

        PresidentId = newPresidentId;
    }

    public Pair CreatePair(PlayerId first, PlayerId second)
    {
        var pair = new Pair(first, second);
        EnsureIsMember(first);
        EnsureIsMember(second);

        if (PairOf(first) is not null || PairOf(second) is not null)
        {
            throw new DomainException("A player can be in only one pair.");
        }

        _pairs.Add(pair);
        return pair;
    }

    public void DissolvePair(PlayerId playerId)
    {
        var pair = PairOf(playerId) ?? throw new DomainException("The player is not in a pair.");
        _pairs.Remove(pair);
    }

    public Pair? PairOf(PlayerId playerId) => _pairs.Find(pair => pair.Contains(playerId));

    /// <summary>The player's partner, or null if the player has no pair.</summary>
    public PlayerId? PartnerOf(PlayerId playerId) => PairOf(playerId)?.PartnerOf(playerId);

    private void EnsureIsMember(PlayerId playerId) => _ = GetMember(playerId);

    private GroupMember? FindMember(PlayerId playerId) => _members.Find(member => member.PlayerId == playerId);
}
