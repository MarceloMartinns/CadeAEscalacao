using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.WeeklyLists;

/// <summary>A player's line on the weekly list.</summary>
/// <param name="PlayerId">The player.</param>
/// <param name="Type">Regular or substitute in the group when the player joined the list.</param>
/// <param name="Status">Pending, confirmed or declined.</param>
/// <param name="Positions">Positions the player chose for this match.</param>
/// <param name="HasReservedSpot">
/// True for regulars until they decline. A regular who declines and comes back loses it
/// and competes for a spot like a substitute.
/// </param>
/// <param name="SignUpOrder">Tie-breaker between substitutes with the same attendance (lower first).</param>
public sealed record WeeklyListEntry(
    PlayerId PlayerId,
    PlayerType Type,
    EntryStatus Status,
    Position Positions,
    bool HasReservedSpot,
    int SignUpOrder);
