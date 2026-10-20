using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Seasons;

namespace CadeAEscalacao.Application.Rankings;

/// <summary>
/// The season ranking. Unlike the other answers, each entry brings the player's name: someone who
/// left the group is no longer among its members, but still is in the ranking of the season (ADR-021).
/// </summary>
public sealed record SeasonRankingResponse(
    Guid SeasonId,
    Guid GroupId,
    string Name,
    DateOnly Start,
    DateOnly End,
    IReadOnlyList<RankingEntryResponse> Entries);

public sealed record RankingEntryResponse(
    int Position,
    Guid PlayerId,
    string PlayerName,
    int Points,
    int Attendances,
    int Wins,
    int Draws,
    int Losses);

public sealed class GetSeasonRanking(
    ISeasonRepository seasons,
    IMatchRepository matches,
    IGroupRepository groups,
    IPlayerRepository players,
    ICurrentUser currentUser)
{
    /// <summary>The season ranking, or null if the season does not exist. Only members of the group can see it.</summary>
    public async Task<SeasonRankingResponse?> HandleAsync(SeasonId seasonId, CancellationToken cancellationToken = default)
    {
        var season = await seasons.GetByIdAsync(seasonId, cancellationToken);
        if (season is null)
        {
            return null;
        }

        var group = await groups.GetByIdAsync(season.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", season.GroupId);
        Roles.EnsureMember(group, currentUser.PlayerId);

        var seasonMatches = await matches.ListByGroupAsync(season.GroupId, season.Start, season.End, cancellationToken);
        var ranking = SeasonRanking.Calculate(season, seasonMatches);
        var names = await players.GetNamesAsync(ranking.Select(entry => entry.PlayerId).ToList(), cancellationToken);
        var entries = ranking
            .Select(entry => new RankingEntryResponse(
                entry.Position,
                entry.PlayerId.Value,
                names.GetValueOrDefault(entry.PlayerId, string.Empty),
                entry.Points,
                entry.Attendances,
                entry.Wins,
                entry.Draws,
                entry.Losses))
            .ToList();

        return new SeasonRankingResponse(season.Id.Value, season.GroupId.Value, season.Name, season.Start, season.End, entries);
    }
}
