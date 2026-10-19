using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Lists;

public sealed class GetWeeklyList(
    IWeeklyListRepository lists, IGroupRepository groups, SeasonAttendance attendance, ICurrentUser currentUser)
{
    /// <summary>The list with the current slot allocation, or null if not found. Only members can see it.</summary>
    public async Task<WeeklyListResponse?> HandleAsync(WeeklyListId id, CancellationToken cancellationToken = default)
    {
        var list = await lists.GetByIdAsync(id, cancellationToken);
        if (list is null)
        {
            return null;
        }

        var group = await groups.GetByIdAsync(list.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", list.GroupId);
        Roles.EnsureMember(group, currentUser.PlayerId);

        return WeeklyListResponse.From(
            list, await attendance.ForDateAsync(list.GroupId, list.MatchDate, cancellationToken));
    }
}
