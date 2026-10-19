using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Lists;

/// <summary>
/// Steps shared by the use cases that change a list: load it with its group, run the change
/// (which checks the role first, ADR-016), save and answer with the new slot allocation.
/// </summary>
public sealed class ChangeWeeklyList(
    IWeeklyListRepository lists,
    IGroupRepository groups,
    SeasonAttendance attendance,
    IUnitOfWork unitOfWork)
{
    public async Task<WeeklyListResponse> HandleAsync(
        WeeklyListId id, Action<WeeklyList, Group> change, CancellationToken cancellationToken = default)
    {
        var list = await lists.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Weekly list", id);
        var group = await groups.GetByIdAsync(list.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", list.GroupId);

        change(list, group);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return WeeklyListResponse.From(
            list, await attendance.ForDateAsync(list.GroupId, list.MatchDate, cancellationToken));
    }
}

/// <summary>The signed-in member confirms presence. The group is needed to know whether they are a regular.</summary>
public sealed class SignUpForList(ChangeWeeklyList changeList, ICurrentUser currentUser)
{
    public Task<WeeklyListResponse> HandleAsync(
        WeeklyListId id, Position positions, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(
            id,
            (list, group) =>
            {
                Roles.EnsureMember(group, currentUser.PlayerId);
                list.SignUp(group, currentUser.PlayerId, positions);
            },
            cancellationToken);
}

/// <summary>The signed-in player declines or leaves the list.</summary>
public sealed class WithdrawFromList(ChangeWeeklyList changeList, ICurrentUser currentUser)
{
    public Task<WeeklyListResponse> HandleAsync(WeeklyListId id, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(id, (list, _) => list.Withdraw(currentUser.PlayerId), cancellationToken);
}

/// <summary>The signed-in player changes their positions.</summary>
public sealed class ChangeListPositions(ChangeWeeklyList changeList, ICurrentUser currentUser)
{
    public Task<WeeklyListResponse> HandleAsync(
        WeeklyListId id, Position positions, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(
            id, (list, _) => list.ChangePositions(currentUser.PlayerId, positions), cancellationToken);
}

public sealed class CloseWeeklyList(ChangeWeeklyList changeList, ICurrentUser currentUser)
{
    public Task<WeeklyListResponse> HandleAsync(WeeklyListId id, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(
            id,
            (list, group) =>
            {
                Roles.EnsurePresident(group, currentUser.PlayerId);
                list.Close();
            },
            cancellationToken);
}

public sealed class CancelWeeklyList(ChangeWeeklyList changeList, ICurrentUser currentUser)
{
    public Task<WeeklyListResponse> HandleAsync(WeeklyListId id, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(
            id,
            (list, group) =>
            {
                Roles.EnsurePresident(group, currentUser.PlayerId);
                list.Cancel();
            },
            cancellationToken);
}
