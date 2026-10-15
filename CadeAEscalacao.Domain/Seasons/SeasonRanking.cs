using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Seasons;

/// <summary>
/// Season ranking of a group: points (win 3, draw 1, loss 0) and attendances.
/// Order: points, then attendances, then wins. Players tied on all three share the position.
/// </summary>
public static class SeasonRanking
{
    public static IReadOnlyList<RankingEntry> Calculate(Season season, IEnumerable<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(season);
        ArgumentNullException.ThrowIfNull(matches);

        var stats = new Dictionary<PlayerId, (int Wins, int Draws, int Losses)>();

        var seasonMatches = matches.Where(match => match.GroupId == season.GroupId && season.Contains(match.Date));
        foreach (var match in seasonMatches)
        {
            foreach (var playerId in match.Players)
            {
                var (wins, draws, losses) = stats.GetValueOrDefault(playerId);
                stats[playerId] = match.ResultOf(playerId) switch
                {
                    MatchResult.Win => (wins + 1, draws, losses),
                    MatchResult.Draw => (wins, draws + 1, losses),
                    _ => (wins, draws, losses + 1),
                };
            }
        }

        var ordered = stats
            .Select(pair => new
            {
                PlayerId = pair.Key,
                Points = (pair.Value.Wins * Scoring.PointsPerWin)
                    + (pair.Value.Draws * Scoring.PointsPerDraw)
                    + (pair.Value.Losses * Scoring.PointsPerLoss),
                Attendances = pair.Value.Wins + pair.Value.Draws + pair.Value.Losses,
                pair.Value.Wins,
                pair.Value.Draws,
                pair.Value.Losses,
            })
            .OrderByDescending(line => line.Points)
            .ThenByDescending(line => line.Attendances)
            .ThenByDescending(line => line.Wins)
            .ToList();

        var ranking = new List<RankingEntry>(ordered.Count);
        for (var index = 0; index < ordered.Count; index++)
        {
            var line = ordered[index];
            var previous = index > 0 ? ranking[index - 1] : null;

            // Same points, attendances and wins as the player above: same position (1, 1, 3...).
            var position = previous is not null
                && previous.Points == line.Points
                && previous.Attendances == line.Attendances
                && previous.Wins == line.Wins
                    ? previous.Position
                    : index + 1;

            ranking.Add(new RankingEntry(
                position, line.PlayerId, line.Points, line.Attendances, line.Wins, line.Draws, line.Losses));
        }

        return ranking;
    }

    /// <summary>
    /// Attendances per player in the season: the input of <see cref="WeeklyLists.WeeklyList.AllocateSlots"/>.
    /// </summary>
    public static IReadOnlyDictionary<PlayerId, int> Attendances(Season season, IEnumerable<Match> matches) =>
        Calculate(season, matches).ToDictionary(entry => entry.PlayerId, entry => entry.Attendances);
}
