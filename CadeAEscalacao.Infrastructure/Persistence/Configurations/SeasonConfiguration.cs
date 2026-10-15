using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Seasons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CadeAEscalacao.Infrastructure.Persistence.Configurations;

internal sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("seasons");

        builder.HasKey(season => season.Id);
        builder.Property(season => season.Id).ValueGeneratedNever();

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(season => season.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(season => season.Name).HasMaxLength(100);

        // "end" is a reserved word in SQL.
        builder.Property(season => season.Start).HasColumnName("start_date");
        builder.Property(season => season.End).HasColumnName("end_date");
    }
}
