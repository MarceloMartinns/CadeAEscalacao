using CadeAEscalacao.Domain.Players;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CadeAEscalacao.Infrastructure.Persistence.Converters;

/// <summary>
/// An ordered list of players (coach rotation, teams) stored as a Postgres <c>uuid[]</c>.
/// The array keeps the order, which matters for the coach rotation.
/// </summary>
internal sealed class PlayerIdListConverter()
    : ValueConverter<List<PlayerId>, Guid[]>(
        list => list.Select(id => id.Value).ToArray(),
        array => array.Select(value => new PlayerId(value)).ToList());

/// <summary>
/// The domain changes these lists in place (Add, Remove, Clear), so EF Core needs to compare
/// the contents (not the reference) and keep a copy as the snapshot to detect the change.
/// </summary>
internal sealed class PlayerIdListComparer()
    : ValueComparer<List<PlayerId>>(
        (left, right) => left!.SequenceEqual(right!),
        list => list.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        list => list.ToList());
