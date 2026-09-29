using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Groups;

/// <summary>
/// Two players defined by the president ("dupla"). When one of them is the coach,
/// the other picks which team to join and the coach takes the other team.
/// </summary>
public sealed record Pair
{
    public Pair(PlayerId first, PlayerId second)
    {
        if (first == second)
        {
            throw new DomainException("A pair needs two different players.");
        }

        First = first;
        Second = second;
    }

    public PlayerId First { get; }

    public PlayerId Second { get; }

    public bool Contains(PlayerId playerId) => First == playerId || Second == playerId;

    public PlayerId PartnerOf(PlayerId playerId)
    {
        if (playerId == First)
        {
            return Second;
        }

        if (playerId == Second)
        {
            return First;
        }

        throw new ArgumentException("The player is not in this pair.", nameof(playerId));
    }
}
