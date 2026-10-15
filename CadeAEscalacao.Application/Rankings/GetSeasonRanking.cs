using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Domain.Seasons;

namespace CadeAEscalacao.Application.Rankings;

public sealed record SeasonRankingResponse(
    Guid SeasonId,
    string Name,
    DateOnly Start,
    DateOnly End,
    IReadOnlyList<RankingEntryResponse> Entries);

public sealed record RankingEntryResponse(
    int Position,
    Guid PlayerId,
    int Points,
    int Attendances,
    int Wins,
    int Draws,
    int Losses);

public sealed class GetSeasonRanking(ISeasonRepository seasons, IMatchRepository matches)
{
    /// <summary>The season ranking, or null if the season does not exist.</summary>
    public async Task<SeasonRankingResponse?> HandleAsync(SeasonId seasonId, CancellationToken cancellationToken = default)
    {
        var season = await seasons.GetByIdAsync(seasonId, cancellationToken);
        if (season is null)
        {
            return null;
        }

        var seasonMatches = await matches.ListByGroupAsync(season.GroupId, season.Start, season.End, cancellationToken);
        var entries = SeasonRanking.Calculate(season, seasonMatches)
            .Select(entry => new RankingEntryResponse(
                entry.Position,
                entry.PlayerId.Value,
                entry.Points,
                entry.Attendances,
                entry.Wins,
                entry.Draws,
                entry.Losses))
            .ToList();

        return new SeasonRankingResponse(season.Id.Value, season.Name, season.Start, season.End, entries);
    }
}
