using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Lists;

/// <summary>The list with who plays, who still holds a reserved spot, who is waiting and who left.</summary>
public sealed record WeeklyListResponse(
    Guid Id,
    Guid GroupId,
    DateOnly MatchDate,
    WeeklyListStatus Status,
    int Capacity,
    int OpenSpots,
    IReadOnlyList<WeeklyListEntryResponse> Playing,
    IReadOnlyList<WeeklyListEntryResponse> Pending,
    IReadOnlyList<WeeklyListEntryResponse> Waiting,
    IReadOnlyList<WeeklyListEntryResponse> Declined)
{
    public static WeeklyListResponse From(WeeklyList list, IReadOnlyDictionary<PlayerId, int> seasonAttendance)
    {
        var allocation = list.AllocateSlots(seasonAttendance);
        return new WeeklyListResponse(
            list.Id.Value,
            list.GroupId.Value,
            list.MatchDate,
            list.Status,
            list.Capacity,
            allocation.OpenSpots,
            ToResponses(allocation.Playing),
            ToResponses(allocation.Pending),
            ToResponses(allocation.Waiting),
            ToResponses(list.Entries.Where(entry => entry.Status == EntryStatus.Declined)));
    }

    private static List<WeeklyListEntryResponse> ToResponses(IEnumerable<WeeklyListEntry> entries) =>
        entries.Select(entry => new WeeklyListEntryResponse(
                entry.PlayerId.Value,
                entry.Type,
                entry.Status,
                Positions.Split(entry.Positions),
                entry.HasReservedSpot,
                entry.SignUpOrder))
            .ToList();
}

public sealed record WeeklyListEntryResponse(
    Guid PlayerId,
    PlayerType Type,
    EntryStatus Status,
    IReadOnlyList<Position> Positions,
    bool HasReservedSpot,
    int SignUpOrder);
