using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;
using CadeAEscalacao.Domain.WeeklyLists;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CadeAEscalacao.Infrastructure.Persistence.Converters;

// Strongly typed ids (ADR-010) are stored as uuid. Registered for every property
// of each type in AppDbContext.ConfigureConventions.

internal sealed class PlayerIdConverter()
    : ValueConverter<PlayerId, Guid>(id => id.Value, value => new PlayerId(value));

internal sealed class GroupIdConverter()
    : ValueConverter<GroupId, Guid>(id => id.Value, value => new GroupId(value));

internal sealed class WeeklyListIdConverter()
    : ValueConverter<WeeklyListId, Guid>(id => id.Value, value => new WeeklyListId(value));

internal sealed class LineupIdConverter()
    : ValueConverter<LineupId, Guid>(id => id.Value, value => new LineupId(value));

internal sealed class MatchIdConverter()
    : ValueConverter<MatchId, Guid>(id => id.Value, value => new MatchId(value));

internal sealed class SeasonIdConverter()
    : ValueConverter<SeasonId, Guid>(id => id.Value, value => new SeasonId(value));
