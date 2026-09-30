using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Seasons;

/// <summary>A player's line in the season ranking.</summary>
/// <param name="Position">1 for the leader. Players tied on every criterion share the position.</param>
public sealed record RankingEntry(
    int Position,
    PlayerId PlayerId,
    int Points,
    int Attendances,
    int Wins,
    int Draws,
    int Losses);
