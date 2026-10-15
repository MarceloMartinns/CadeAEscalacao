using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;

namespace CadeAEscalacao.Application.Rankings;

/// <summary>
/// Attendances per player in the group's season that contains a date: the input of
/// <see cref="Domain.WeeklyLists.WeeklyList.AllocateSlots"/> and <see cref="Domain.Lineups.Lineup.Start"/>.
/// With no season for the date, everybody has zero attendances (substitutes go by sign-up order).
/// </summary>
public sealed class SeasonAttendance(ISeasonRepository seasons, IMatchRepository matches)
{
    public async Task<IReadOnlyDictionary<PlayerId, int>> ForDateAsync(
        GroupId groupId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var season = await seasons.FindByDateAsync(groupId, date, cancellationToken);
        if (season is null)
        {
            return new Dictionary<PlayerId, int>();
        }

        var seasonMatches = await matches.ListByGroupAsync(groupId, season.Start, season.End, cancellationToken);
        return SeasonRanking.Attendances(season, seasonMatches);
    }
}
