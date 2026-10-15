using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CadeAEscalacao.Infrastructure.Persistence.Configurations;

internal sealed class WeeklyListConfiguration : IEntityTypeConfiguration<WeeklyList>
{
    public void Configure(EntityTypeBuilder<WeeklyList> builder)
    {
        builder.ToTable("weekly_lists");

        builder.HasKey(list => list.Id);
        builder.Property(list => list.Id).ValueGeneratedNever();

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(list => list.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // One list per group per match day.
        builder.HasIndex(list => new { list.GroupId, list.MatchDate }).IsUnique();

        // Get-only property: EF Core only maps it when configured explicitly.
        builder.Property(list => list.Capacity);

        builder.OwnsMany(list => list.Entries, entry =>
        {
            entry.ToTable("weekly_list_entries");
            entry.WithOwner().HasForeignKey("WeeklyListId");
            entry.HasKey("WeeklyListId", nameof(WeeklyListEntry.PlayerId));

            entry.HasOne<Player>()
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(list => list.Entries).HasField("_entries");
    }
}
