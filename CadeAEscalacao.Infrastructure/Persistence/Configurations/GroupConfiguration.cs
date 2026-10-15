using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CadeAEscalacao.Infrastructure.Persistence.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(group => group.Id);
        builder.Property(group => group.Id).ValueGeneratedNever();

        builder.Property(group => group.Name).HasMaxLength(100);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(group => group.PresidentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Value object: two columns in the groups table.
        builder.ComplexProperty(group => group.Format, format =>
        {
            format.Property(f => f.PlayersPerTeam).HasColumnName("players_per_team");
            format.Property(f => f.BenchPlayersPerTeam).HasColumnName("bench_players_per_team");
        });

        builder.OwnsMany(group => group.Members, member =>
        {
            member.ToTable("group_members");
            member.WithOwner().HasForeignKey("GroupId");
            member.HasKey("GroupId", nameof(GroupMember.PlayerId));

            member.HasOne<Player>()
                .WithMany()
                .HasForeignKey(m => m.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(group => group.Members).HasField("_members");

        builder.OwnsMany(group => group.Pairs, pair =>
        {
            pair.ToTable("group_pairs");
            pair.WithOwner().HasForeignKey("GroupId");

            // A player is in at most one pair (Group.CreatePair): unique on both sides.
            pair.HasKey("GroupId", nameof(Pair.First));
            pair.HasIndex("GroupId", nameof(Pair.Second)).IsUnique();

            pair.Property(p => p.First).HasColumnName("first_player_id");
            pair.Property(p => p.Second).HasColumnName("second_player_id");

            pair.HasOne<Player>()
                .WithMany()
                .HasForeignKey(p => p.First)
                .OnDelete(DeleteBehavior.Restrict);

            pair.HasOne<Player>()
                .WithMany()
                .HasForeignKey(p => p.Second)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(group => group.Pairs).HasField("_pairs");

        // The order is the rotation itself, so it is stored as an array (uuid[]).
        builder.Ignore(group => group.CoachRotation);
        builder.Property<List<PlayerId>>("_coachRotation")
            .HasColumnName("coach_rotation")
            .HasConversion(new PlayerIdListConverter(), new PlayerIdListComparer());
    }
}
