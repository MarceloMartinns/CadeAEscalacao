using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Lineups;

/// <summary>Starts the lineup of a closed list: picks the players and the coach of the week.</summary>
public sealed class StartLineup(
    IWeeklyListRepository lists,
    IGroupRepository groups,
    ILineupRepository lineups,
    SeasonAttendance attendance,
    IUnitOfWork unitOfWork)
{
    public async Task<LineupResponse> HandleAsync(WeeklyListId weeklyListId, CancellationToken cancellationToken = default)
    {
        var list = await lists.GetByIdAsync(weeklyListId, cancellationToken)
            ?? throw new NotFoundException("Weekly list", weeklyListId);
        if (await lineups.ExistsForListAsync(weeklyListId, cancellationToken))
        {
            throw new ConflictException("The list already has a lineup.");
        }

        var group = await groups.GetByIdAsync(list.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group", list.GroupId);
        var seasonAttendance = await attendance.ForDateAsync(list.GroupId, list.MatchDate, cancellationToken);

        var lineup = Lineup.Start(group, list, seasonAttendance);
        lineups.Add(lineup);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return LineupResponse.From(lineup);
    }
}
