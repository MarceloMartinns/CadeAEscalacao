using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Common;

/// <summary>
/// <see cref="Position"/> is a flags enum in the domain; the API uses a list of positions
/// (<c>["Midfielder", "Forward"]</c>), which is easier for the client than "Midfielder, Forward".
/// </summary>
public static class Positions
{
    public static Position Combine(IEnumerable<Position> positions) =>
        positions.Aggregate(Position.None, (all, position) => all | position);

    public static IReadOnlyList<Position> Split(Position positions) =>
        Enum.GetValues<Position>()
            .Where(position => position != Position.None && positions.HasFlag(position))
            .ToList();
}
