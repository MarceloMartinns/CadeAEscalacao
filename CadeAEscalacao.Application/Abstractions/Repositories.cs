using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Application.Abstractions;

// One repository per aggregate root (ADR-010). Implemented with EF Core in the Infrastructure.
// Add only marks the aggregate as new; nothing is written until IUnitOfWork.SaveChangesAsync.

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(PlayerId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(PlayerId id, CancellationToken cancellationToken = default);

    void Add(Player player);
}

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(GroupId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(GroupId id, CancellationToken cancellationToken = default);

    void Add(Group group);
}

public interface IWeeklyListRepository
{
    Task<WeeklyList?> GetByIdAsync(WeeklyListId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(GroupId groupId, DateOnly matchDate, CancellationToken cancellationToken = default);

    void Add(WeeklyList list);
}

public interface ILineupRepository
{
    Task<Lineup?> GetByIdAsync(LineupId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForListAsync(WeeklyListId weeklyListId, CancellationToken cancellationToken = default);

    void Add(Lineup lineup);
}

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(MatchId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForLineupAsync(LineupId lineupId, CancellationToken cancellationToken = default);

    /// <summary>Matches of the group played between <paramref name="from"/> and <paramref name="to"/> (inclusive).</summary>
    Task<IReadOnlyList<Match>> ListByGroupAsync(
        GroupId groupId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    void Add(Match match);
}

public interface ISeasonRepository
{
    Task<Season?> GetByIdAsync(SeasonId id, CancellationToken cancellationToken = default);

    /// <summary>The group's season that contains the date, or null.</summary>
    Task<Season?> FindByDateAsync(GroupId groupId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>True if the group has a season that shares at least one day with the period.</summary>
    Task<bool> OverlapsAsync(GroupId groupId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    void Add(Season season);
}
