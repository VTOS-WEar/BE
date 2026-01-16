using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SizeChartConfiguration : IEntityTypeConfiguration<SizeChart>
{
    public void Configure(EntityTypeBuilder<SizeChart> builder)
    {
        builder.ToTable("SizeChart");

        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Id).HasColumnName("SizeChartID");

        builder.Property(sc => sc.ChartName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(sc => sc.Description)
            .HasMaxLength(500);

        builder.Property(sc => sc.Unit)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("cm");

        // Relationships
        builder.HasMany(sc => sc.SizeChartDetails)
            .WithOne(scd => scd.SizeChart)
            .HasForeignKey(scd => scd.SizeChartID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

