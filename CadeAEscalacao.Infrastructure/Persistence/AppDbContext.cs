using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;
using CadeAEscalacao.Domain.WeeklyLists;
using CadeAEscalacao.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CadeAEscalacao.Infrastructure.Persistence;

/// <summary>
/// One DbSet per aggregate root. Members, pairs and list entries are owned by their aggregate
/// and are only reached through it. Mappings live in <c>Configurations/</c> (ADR-012).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    /// <summary>
    /// Optimistic concurrency token (shadow property) of every aggregate root.
    /// See <see cref="IncrementVersionOfChangedAggregates"/>.
    /// </summary>
    internal const string VersionProperty = "Version";

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<WeeklyList> WeeklyLists => Set<WeeklyList>();

    public DbSet<Lineup> Lineups => Set<Lineup>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<Season> Seasons => Set<Season>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        IncrementVersionOfChangedAggregates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        IncrementVersionOfChangedAggregates();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<PlayerId>().HaveConversion<PlayerIdConverter>();
        configurationBuilder.Properties<GroupId>().HaveConversion<GroupIdConverter>();
        configurationBuilder.Properties<WeeklyListId>().HaveConversion<WeeklyListIdConverter>();
        configurationBuilder.Properties<LineupId>().HaveConversion<LineupIdConverter>();
        configurationBuilder.Properties<MatchId>().HaveConversion<MatchIdConverter>();
        configurationBuilder.Properties<SeasonId>().HaveConversion<SeasonIdConverter>();

        // Value object, never an entity: without this, Group is discovered by convention
        // (through HasOne<Group>() in other configurations) before GroupConfiguration runs,
        // and MatchFormat becomes an entity type without a key.
        configurationBuilder.ComplexProperties<MatchFormat>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        var aggregateRoots = modelBuilder.Model.GetEntityTypes().Where(type => !type.IsOwned()).ToList();
        foreach (var entityType in aggregateRoots)
        {
            modelBuilder.Entity(entityType.ClrType).Property<int>(VersionProperty).IsConcurrencyToken();
        }
    }

    /// <summary>
    /// Bumps the version of every aggregate that changed, including when only an owned child
    /// changed (e.g. a new entry on the weekly list only inserts a row in <c>weekly_list_entries</c>).
    /// The UPDATE runs with <c>WHERE version = @original</c>: if two people sign up for the same list
    /// at the same time, the second save fails with <see cref="DbUpdateConcurrencyException"/>
    /// instead of both being accepted beyond the list's capacity.
    /// </summary>
    private void IncrementVersionOfChangedAggregates()
    {
        ChangeTracker.DetectChanges();

        var entries = ChangeTracker.Entries().ToList();
        var changedRoots = new HashSet<EntityEntry>();

        foreach (var entry in entries)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var root = entry.Metadata.IsOwned() ? FindOwner(entry, entries) : entry;

            // New aggregates start at version 0; deleted ones already check the version in the DELETE.
            if (root is { State: EntityState.Unchanged or EntityState.Modified })
            {
                changedRoots.Add(root);
            }
        }

        foreach (var root in changedRoots)
        {
            var version = root.Property(VersionProperty);
            version.CurrentValue = (int)version.OriginalValue! + 1;
        }
    }

    private static EntityEntry? FindOwner(EntityEntry owned, IReadOnlyList<EntityEntry> entries)
    {
        var ownership = owned.Metadata.FindOwnership()!;
        var ownerKey = ownership.Properties.Select(property => owned.Property(property.Name).CurrentValue).ToList();

        return entries.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Metadata, ownership.PrincipalEntityType)
            && ownership.PrincipalKey.Properties
                .Select(property => candidate.Property(property.Name).CurrentValue)
                .SequenceEqual(ownerKey));
    }
}
