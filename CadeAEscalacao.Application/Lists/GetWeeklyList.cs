using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Lists;

public sealed class GetWeeklyList(IWeeklyListRepository lists, SeasonAttendance attendance)
{
    /// <summary>The list with the current slot allocation, or null if not found.</summary>
    public async Task<WeeklyListResponse?> HandleAsync(WeeklyListId id, CancellationToken cancellationToken = default)
    {
        var list = await lists.GetByIdAsync(id, cancellationToken);
        if (list is null)
        {
            return null;
        }

        return WeeklyListResponse.From(
            list, await attendance.ForDateAsync(list.GroupId, list.MatchDate, cancellationToken));
    }
}
