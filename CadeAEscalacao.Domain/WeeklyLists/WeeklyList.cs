using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.WeeklyLists;

/// <summary>
/// The list for one match ("lista semanal"). Aggregate root.
/// </summary>
/// <remarks>
/// Rules:
/// <list type="bullet">
/// <item>Capacity = 2 × (players + bench) of the group's format when the list opens.</item>
/// <item>Every regular starts with a reserved spot, held until they decline (no deadline).</item>
/// <item>Substitutes fill the remaining spots: most season attendances first, then sign-up order.</item>
/// <item>A regular who declines and comes back only plays if there is a spot (competes like a substitute).</item>
/// <item>Closing the list: regulars who did not answer lose their spot.</item>
/// </list>
/// </remarks>
public sealed class WeeklyList
{
    private readonly List<WeeklyListEntry> _entries = [];

    private WeeklyList(WeeklyListId id, GroupId groupId, DateOnly matchDate, int capacity)
    {
        Id = id;
        GroupId = groupId;
        MatchDate = matchDate;
        Capacity = capacity;
        Status = WeeklyListStatus.Open;
    }

    public WeeklyListId Id { get; }

    public GroupId GroupId { get; }

    public DateOnly MatchDate { get; }

    public int Capacity { get; }

    public WeeklyListStatus Status { get; private set; }

    public IReadOnlyList<WeeklyListEntry> Entries => _entries;

    /// <summary>Opens the list with every regular of the group pending (spot reserved).</summary>
    public static WeeklyList Open(Group group, DateOnly matchDate)
    {
        ArgumentNullException.ThrowIfNull(group);

        var list = new WeeklyList(WeeklyListId.New(), group.Id, matchDate, group.Format.TotalSpots);

        foreach (var member in group.Members.Where(member => member.IsRegular))
        {
            list._entries.Add(new WeeklyListEntry(
                member.PlayerId, PlayerType.Regular, EntryStatus.Pending, Position.None, HasReservedSpot: true, SignUpOrder: 0));
        }

        return list;
    }

    /// <summary>
    /// A member confirms presence with the positions they want to play.
    /// The group is used to check membership and the player's type.
    /// </summary>
    public void SignUp(Group group, PlayerId playerId, Position positions)
    {
        ArgumentNullException.ThrowIfNull(group);
        EnsureOpen();
        EnsureHasPosition(positions);

        if (group.Id != GroupId)
        {
            throw new DomainException("This list belongs to another group.");
        }

        var member = group.GetMember(playerId);
        var entry = FindEntry(playerId);
        var order = NextSignUpOrder();

        if (entry is null)
        {
            // Substitute, or a regular who joined the group after the list opened.
            _entries.Add(new WeeklyListEntry(
                playerId, member.Type, EntryStatus.Confirmed, positions, HasReservedSpot: member.IsRegular, order));
            return;
        }

        switch (entry.Status)
        {
            case EntryStatus.Pending:
                Replace(entry, entry with { Status = EntryStatus.Confirmed, Positions = positions, SignUpOrder = order });
                break;

            case EntryStatus.Declined:
                // Came back after declining: no reserved spot, competes like a substitute.
                Replace(entry, entry with
                {
                    Status = EntryStatus.Confirmed,
                    Positions = positions,
                    HasReservedSpot = false,
                    SignUpOrder = order,
                });
                break;

            default:
                throw new DomainException("The player is already on the list.");
        }
    }

    /// <summary>The player declines (regular) or leaves the list. A regular loses the reserved spot.</summary>
    public void Withdraw(PlayerId playerId)
    {
        EnsureOpen();
        var entry = GetEntry(playerId);

        if (entry.Status == EntryStatus.Declined)
        {
            throw new DomainException("The player has already left the list.");
        }

        Replace(entry, entry with { Status = EntryStatus.Declined, HasReservedSpot = false });
    }

    public void ChangePositions(PlayerId playerId, Position positions)
    {
        EnsureOpen();
        EnsureHasPosition(positions);
        var entry = GetEntry(playerId);

        if (entry.Status != EntryStatus.Confirmed)
        {
            throw new DomainException("Only confirmed players can change positions.");
        }

        Replace(entry, entry with { Positions = positions });
    }

    /// <summary>Closes the list. Regulars who did not answer lose their spot.</summary>
    public void Close()
    {
        EnsureOpen();

        foreach (var entry in _entries.Where(e => e.Status == EntryStatus.Pending).ToList())
        {
            Replace(entry, entry with { Status = EntryStatus.Declined, HasReservedSpot = false });
        }

        Status = WeeklyListStatus.Closed;
    }

    public void Cancel()
    {
        if (Status == WeeklyListStatus.Cancelled)
        {
            throw new DomainException("The list is already cancelled.");
        }

        Status = WeeklyListStatus.Cancelled;
    }

    /// <summary>
    /// Who gets a spot right now. Season attendance comes from outside (ADR-010);
    /// a player missing from the dictionary has zero attendances.
    /// </summary>
    public SlotAllocation AllocateSlots(IReadOnlyDictionary<PlayerId, int> seasonAttendance)
    {
        ArgumentNullException.ThrowIfNull(seasonAttendance);

        var reserved = _entries.Where(entry => entry.HasReservedSpot).ToList();
        var spotsForOthers = Math.Max(0, Capacity - reserved.Count);

        var candidates = _entries
            .Where(entry => !entry.HasReservedSpot && entry.Status == EntryStatus.Confirmed)
            .OrderByDescending(entry => seasonAttendance.GetValueOrDefault(entry.PlayerId))
            .ThenBy(entry => entry.SignUpOrder)
            .ToList();

        var playing = reserved
            .Where(entry => entry.Status == EntryStatus.Confirmed)
            .Concat(candidates.Take(spotsForOthers))
            .ToList();

        var pending = reserved.Where(entry => entry.Status == EntryStatus.Pending).ToList();
        var waiting = candidates.Skip(spotsForOthers).ToList();

        return new SlotAllocation(Capacity, playing, pending, waiting);
    }

    private void EnsureOpen()
    {
        if (Status != WeeklyListStatus.Open)
        {
            throw new DomainException("The list is not open.");
        }
    }

    private static void EnsureHasPosition(Position positions)
    {
        if (positions == Position.None)
        {
            throw new ArgumentException("Choose at least one position.", nameof(positions));
        }
    }

    private WeeklyListEntry? FindEntry(PlayerId playerId) => _entries.Find(entry => entry.PlayerId == playerId);

    private WeeklyListEntry GetEntry(PlayerId playerId) =>
        FindEntry(playerId) ?? throw new DomainException("The player is not on the list.");

    private int NextSignUpOrder() => _entries.Count == 0 ? 1 : _entries.Max(entry => entry.SignUpOrder) + 1;

    private void Replace(WeeklyListEntry current, WeeklyListEntry updated) =>
        _entries[_entries.IndexOf(current)] = updated;
}
