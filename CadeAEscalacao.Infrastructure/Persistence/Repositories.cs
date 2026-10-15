using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;
using CadeAEscalacao.Domain.WeeklyLists;
using Microsoft.EntityFrameworkCore;

namespace CadeAEscalacao.Infrastructure.Persistence;

// Owned collections (members, pairs, list entries) are loaded with their aggregate automatically.

internal sealed class PlayerRepository(AppDbContext db) : IPlayerRepository
{
    public Task<Player?> GetByIdAsync(PlayerId id, CancellationToken cancellationToken = default) =>
        db.Players.FirstOrDefaultAsync(player => player.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(PlayerId id, CancellationToken cancellationToken = default) =>
        db.Players.AnyAsync(player => player.Id == id, cancellationToken);

    public void Add(Player player) => db.Players.Add(player);
}

internal sealed class GroupRepository(AppDbContext db) : IGroupRepository
{
    public Task<Group?> GetByIdAsync(GroupId id, CancellationToken cancellationToken = default) =>
        db.Groups.FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(GroupId id, CancellationToken cancellationToken = default) =>
        db.Groups.AnyAsync(group => group.Id == id, cancellationToken);

    public void Add(Group group) => db.Groups.Add(group);
}

internal sealed class WeeklyListRepository(AppDbContext db) : IWeeklyListRepository
{
    public Task<WeeklyList?> GetByIdAsync(WeeklyListId id, CancellationToken cancellationToken = default) =>
        db.WeeklyLists.FirstOrDefaultAsync(list => list.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(GroupId groupId, DateOnly matchDate, CancellationToken cancellationToken = default) =>
        db.WeeklyLists.AnyAsync(list => list.GroupId == groupId && list.MatchDate == matchDate, cancellationToken);

    public void Add(WeeklyList list) => db.WeeklyLists.Add(list);
}

internal sealed class LineupRepository(AppDbContext db) : ILineupRepository
{
    public Task<Lineup?> GetByIdAsync(LineupId id, CancellationToken cancellationToken = default) =>
        db.Lineups.FirstOrDefaultAsync(lineup => lineup.Id == id, cancellationToken);

    public Task<bool> ExistsForListAsync(WeeklyListId weeklyListId, CancellationToken cancellationToken = default) =>
        db.Lineups.AnyAsync(lineup => lineup.WeeklyListId == weeklyListId, cancellationToken);

    public void Add(Lineup lineup) => db.Lineups.Add(lineup);
}

internal sealed class MatchRepository(AppDbContext db) : IMatchRepository
{
    public Task<Match?> GetByIdAsync(MatchId id, CancellationToken cancellationToken = default) =>
        db.Matches.FirstOrDefaultAsync(match => match.Id == id, cancellationToken);

    public Task<bool> ExistsForLineupAsync(LineupId lineupId, CancellationToken cancellationToken = default) =>
        db.Matches.AnyAsync(match => match.LineupId == lineupId, cancellationToken);

    public async Task<IReadOnlyList<Match>> ListByGroupAsync(
        GroupId groupId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await db.Matches
            .Where(match => match.GroupId == groupId && match.Date >= from && match.Date <= to)
            .ToListAsync(cancellationToken);

    public void Add(Match match) => db.Matches.Add(match);
}

internal sealed class SeasonRepository(AppDbContext db) : ISeasonRepository
{
    public Task<Season?> GetByIdAsync(SeasonId id, CancellationToken cancellationToken = default) =>
        db.Seasons.FirstOrDefaultAsync(season => season.Id == id, cancellationToken);

    public Task<Season?> FindByDateAsync(GroupId groupId, DateOnly date, CancellationToken cancellationToken = default) =>
        db.Seasons
            .Where(season => season.GroupId == groupId && season.Start <= date && season.End >= date)
            .OrderByDescending(season => season.Start)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> OverlapsAsync(
        GroupId groupId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        db.Seasons.AnyAsync(
            season => season.GroupId == groupId && season.Start <= end && season.End >= start,
            cancellationToken);

    public void Add(Season season) => db.Seasons.Add(season);
}
