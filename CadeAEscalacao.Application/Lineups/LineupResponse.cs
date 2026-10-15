using CadeAEscalacao.Domain.Lineups;

namespace CadeAEscalacao.Application.Lineups;

public sealed record LineupResponse(
    Guid Id,
    Guid GroupId,
    Guid WeeklyListId,
    LineupStatus Status,
    Guid CoachId,
    Guid? PartnerId,
    IReadOnlyList<Guid> Players,
    IReadOnlyList<Guid> TeamA,
    IReadOnlyList<Guid> TeamB)
{
    public static LineupResponse From(Lineup lineup) => new(
        lineup.Id.Value,
        lineup.GroupId.Value,
        lineup.WeeklyListId.Value,
        lineup.Status,
        lineup.CoachId.Value,
        lineup.PartnerId?.Value,
        lineup.Players.Select(playerId => playerId.Value).ToList(),
        lineup.TeamA.Select(playerId => playerId.Value).ToList(),
        lineup.TeamB.Select(playerId => playerId.Value).ToList());
}
