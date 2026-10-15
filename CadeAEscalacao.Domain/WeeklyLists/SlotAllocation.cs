namespace CadeAEscalacao.Domain.WeeklyLists;

/// <summary>Who plays, who still holds a reserved spot and who is waiting, at a given moment.</summary>
/// <param name="Capacity">Total spots on the list.</param>
/// <param name="Playing">Confirmed regulars plus the substitutes who got a spot.</param>
/// <param name="Pending">Regulars who have not answered yet: their spots are held.</param>
/// <param name="Waiting">Confirmed players without a spot, in priority order.</param>
public sealed record SlotAllocation(
    int Capacity,
    IReadOnlyList<WeeklyListEntry> Playing,
    IReadOnlyList<WeeklyListEntry> Pending,
    IReadOnlyList<WeeklyListEntry> Waiting)
{
    public int OpenSpots => Math.Max(0, Capacity - Playing.Count - Pending.Count);

    public bool IsFull => OpenSpots == 0;
}
