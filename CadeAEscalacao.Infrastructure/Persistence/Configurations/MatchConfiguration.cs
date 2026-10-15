using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CadeAEscalacao.Infrastructure.Persistence.Configurations;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");

        builder.HasKey(match => match.Id);
        builder.Property(match => match.Id).ValueGeneratedNever();

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(match => match.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // One match per lineup.
        builder.HasOne<Lineup>()
            .WithMany()
            .HasForeignKey(match => match.LineupId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(match => match.LineupId).IsUnique();

        // The ranking filters matches by group and season dates.
        builder.Property(match => match.Date).HasColumnName("match_date");
        builder.HasIndex(match => new { match.GroupId, match.Date });

        builder.Ignore(match => match.Players);
        builder.Ignore(match => match.TeamA);
        builder.Ignore(match => match.TeamB);

        builder.Property<List<PlayerId>>("_teamA")
            .HasColumnName("team_a")
            .HasConversion(new PlayerIdListConverter(), new PlayerIdListComparer());

        builder.Property<List<PlayerId>>("_teamB")
            .HasColumnName("team_b")
            .HasConversion(new PlayerIdListConverter(), new PlayerIdListComparer());
    }
}
