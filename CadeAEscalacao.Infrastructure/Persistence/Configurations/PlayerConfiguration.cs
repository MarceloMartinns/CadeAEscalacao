using CadeAEscalacao.Domain.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CadeAEscalacao.Infrastructure.Persistence.Configurations;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");

        builder.HasKey(player => player.Id);
        builder.Property(player => player.Id).ValueGeneratedNever();

        builder.Property(player => player.Name).HasMaxLength(100);
    }
}
