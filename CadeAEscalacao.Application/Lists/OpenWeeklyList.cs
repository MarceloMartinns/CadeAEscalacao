using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Lists;

/// <summary>The president opens the list of a match date, with every regular of the group pending.</summary>
public sealed class OpenWeeklyList(
    IGroupRepository groups,
    IWeeklyListRepository lists,
    SeasonAttendance attendance,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<WeeklyListResponse> HandleAsync(
        GroupId groupId, DateOnly matchDate, CancellationToken cancellationToken = default)
    {
        var group = await groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", groupId);
        Roles.EnsurePresident(group, currentUser.PlayerId);
        if (await lists.ExistsAsync(groupId, matchDate, cancellationToken))
        {
            throw new ConflictException("The group already has a list for this date.");
        }

        var list = WeeklyList.Open(group, matchDate);
        lists.Add(list);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return WeeklyListResponse.From(list, await attendance.ForDateAsync(groupId, matchDate, cancellationToken));
    }
}
