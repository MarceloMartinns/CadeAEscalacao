using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Groups;

/// <summary>
/// A weekly game slot ("horário"): format, match day, members, president, pairs and coach rotation.
/// Aggregate root: members, pairs and the rotation only change through its methods,
/// so the rules below always hold.
/// </summary>
/// <remarks>
/// Rules:
/// <list type="bullet">
/// <item>The president is always a regular member.</item>
/// <item>A pair has two different members, and a player is in at most one pair.</item>
/// <item>Removing a member dissolves their pair.</item>
/// <item>The coach rotation has every regular member exactly once, and nobody else.</item>
/// </list>
/// </remarks>
public sealed class Group
{
    private readonly List<GroupMember> _members = [];
    private readonly List<Pair> _pairs = [];
    private readonly List<PlayerId> _coachRotation = [];

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

    /// <summary>
    /// Coach queue ("rodízio de professores"). The first player in the queue who is
    /// present coaches the week and then goes to the end of the queue.
    /// </summary>
    public IReadOnlyList<PlayerId> CoachRotation => _coachRotation;

    /// <summary>Creates the group with the president as its first regular member.</summary>
    public static Group Create(string name, MatchFormat format, DayOfWeek matchDay, PlayerId presidentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(format);

        var group = new Group(GroupId.New(), name.Trim(), format, matchDay, presidentId);
        group._members.Add(new GroupMember(presidentId, PlayerType.Regular));
        group._coachRotation.Add(presidentId);
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

    /// <summary>Adds a member. A regular member goes to the end of the coach rotation.</summary>
    public void AddMember(PlayerId playerId, PlayerType type)
    {
        if (IsMember(playerId))
        {
            throw new DomainException("The player is already a member of this group.");
        }

        _members.Add(new GroupMember(playerId, type));

        if (type == PlayerType.Regular)
        {
            _coachRotation.Add(playerId);
        }
    }

    /// <summary>
    /// Changes the member's type. Becoming a substitute removes the player from the coach rotation;
    /// becoming a regular puts the player at the end of it.
    /// </summary>
    public void ChangeMemberType(PlayerId playerId, PlayerType type)
    {
        var member = GetMember(playerId);

        if (playerId == PresidentId && type != PlayerType.Regular)
        {
            throw new DomainException("The president must be a regular player.");
        }

        _members[_members.IndexOf(member)] = member with { Type = type };

        var isRegularNow = type == PlayerType.Regular;
        if (isRegularNow && !member.IsRegular)
        {
            _coachRotation.Add(playerId);
        }
        else if (!isRegularNow && member.IsRegular)
        {
            _coachRotation.Remove(playerId);
        }
    }

    public void RemoveMember(PlayerId playerId)
    {
        if (playerId == PresidentId)
        {
            throw new DomainException("The president cannot leave the group. Transfer the presidency first.");
        }

        var member = GetMember(playerId);
        _pairs.RemoveAll(pair => pair.Contains(playerId));
        _coachRotation.Remove(playerId);
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

    /// <summary>
    /// The president sets the coach order. It must list every regular member exactly once.
    /// </summary>
    public void ReorderCoachRotation(IReadOnlyList<PlayerId> order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var regulars = _members.Where(member => member.IsRegular).Select(member => member.PlayerId).ToHashSet();

        // Same size + same elements = no duplicates, nobody missing, nobody extra.
        if (order.Count != regulars.Count || !regulars.SetEquals(order))
        {
            throw new DomainException("The coach rotation must list every regular member exactly once.");
        }

        _coachRotation.Clear();
        _coachRotation.AddRange(order);
    }

    /// <summary>
    /// This week's coach: the first player in the rotation who is present,
    /// or null if nobody in the rotation is present.
    /// </summary>
    public PlayerId? FindCoach(IEnumerable<PlayerId> presentPlayers)
    {
        ArgumentNullException.ThrowIfNull(presentPlayers);

        var present = presentPlayers.ToHashSet();
        foreach (var playerId in _coachRotation)
        {
            if (present.Contains(playerId))
            {
                return playerId;
            }
        }

        return null;
    }

    /// <summary>
    /// The coach made the lineup: moves them to the end of the rotation.
    /// Players who were skipped (absent) keep their place and coach the next time they play.
    /// </summary>
    public void CompleteCoachTurn(PlayerId coachId)
    {
        if (!_coachRotation.Remove(coachId))
        {
            throw new DomainException("The player is not in the coach rotation.");
        }

        _coachRotation.Add(coachId);
    }

    private void EnsureIsMember(PlayerId playerId) => _ = GetMember(playerId);

    private GroupMember? FindMember(PlayerId playerId) => _members.Find(member => member.PlayerId == playerId);
}
