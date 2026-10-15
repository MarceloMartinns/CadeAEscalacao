using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;
using CadeAEscalacao.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CadeAEscalacao.Infrastructure.Persistence.Configurations;

internal sealed class LineupConfiguration : IEntityTypeConfiguration<Lineup>
{
    public void Configure(EntityTypeBuilder<Lineup> builder)
    {
        builder.ToTable("lineups");

        builder.HasKey(lineup => lineup.Id);
        builder.Property(lineup => lineup.Id).ValueGeneratedNever();

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(lineup => lineup.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // One lineup per list.
        builder.HasOne<WeeklyList>()
            .WithMany()
            .HasForeignKey(lineup => lineup.WeeklyListId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(lineup => lineup.WeeklyListId).IsUnique();

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(lineup => lineup.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        // PlayerId? (nullable): converter set explicitly, the convention covers PlayerId.
        builder.Property(lineup => lineup.PartnerId).HasConversion(new PlayerIdConverter());
        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(lineup => lineup.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(lineup => lineup.Players);
        builder.Ignore(lineup => lineup.TeamA);
        builder.Ignore(lineup => lineup.TeamB);

        builder.Property<List<PlayerId>>("_players")
            .HasColumnName("players")
            .HasConversion(new PlayerIdListConverter(), new PlayerIdListComparer());

        builder.Property<List<PlayerId>>("_teamA")
            .HasColumnName("team_a")
            .HasConversion(new PlayerIdListConverter(), new PlayerIdListComparer());

        builder.Property<List<PlayerId>>("_teamB")
            .HasColumnName("team_b")
            .HasConversion(new PlayerIdListConverter(), new PlayerIdListComparer());
    }
}
