using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Lists;

/// <summary>
/// Steps shared by the use cases that change a list: load it, run the change, save
/// and answer with the new slot allocation.
/// </summary>
public sealed class ChangeWeeklyList(IWeeklyListRepository lists, SeasonAttendance attendance, IUnitOfWork unitOfWork)
{
    public Task<WeeklyListResponse> HandleAsync(
        WeeklyListId id, Action<WeeklyList> change, CancellationToken cancellationToken = default) =>
        HandleAsync(
            id,
            (list, _) =>
            {
                change(list);
                return Task.CompletedTask;
            },
            cancellationToken);

    public async Task<WeeklyListResponse> HandleAsync(
        WeeklyListId id, Func<WeeklyList, CancellationToken, Task> change, CancellationToken cancellationToken = default)
    {
        var list = await lists.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Weekly list", id);

        await change(list, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return WeeklyListResponse.From(
            list, await attendance.ForDateAsync(list.GroupId, list.MatchDate, cancellationToken));
    }
}

/// <summary>A member confirms presence. The group is needed to know whether they are a regular.</summary>
public sealed class SignUpForList(IGroupRepository groups, ChangeWeeklyList changeList)
{
    public Task<WeeklyListResponse> HandleAsync(
        WeeklyListId id, PlayerId playerId, Position positions, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(
            id,
            async (list, token) =>
            {
                var group = await groups.GetByIdAsync(list.GroupId, token)
                    ?? throw new NotFoundException("Group", list.GroupId);
                list.SignUp(group, playerId, positions);
            },
            cancellationToken);
}

public sealed class WithdrawFromList(ChangeWeeklyList changeList)
{
    public Task<WeeklyListResponse> HandleAsync(
        WeeklyListId id, PlayerId playerId, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(id, list => list.Withdraw(playerId), cancellationToken);
}

public sealed class ChangeListPositions(ChangeWeeklyList changeList)
{
    public Task<WeeklyListResponse> HandleAsync(
        WeeklyListId id, PlayerId playerId, Position positions, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(id, list => list.ChangePositions(playerId, positions), cancellationToken);
}

public sealed class CloseWeeklyList(ChangeWeeklyList changeList)
{
    public Task<WeeklyListResponse> HandleAsync(WeeklyListId id, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(id, list => list.Close(), cancellationToken);
}

public sealed class CancelWeeklyList(ChangeWeeklyList changeList)
{
    public Task<WeeklyListResponse> HandleAsync(WeeklyListId id, CancellationToken cancellationToken = default) =>
        changeList.HandleAsync(id, list => list.Cancel(), cancellationToken);
}
