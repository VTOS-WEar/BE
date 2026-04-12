using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SizeChartDetailConfiguration : IEntityTypeConfiguration<SizeChartDetail>
{
    public void Configure(EntityTypeBuilder<SizeChartDetail> builder)
    {
        builder.ToTable("SizeChartDetail");

        builder.HasKey(scd => scd.Id);
        builder.Property(scd => scd.Id).HasColumnName("DetailID");

        builder.Property(scd => scd.SizeChartID)
            .IsRequired();

        builder.Property(scd => scd.SizeLabel)
            .IsRequired()
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(scd => scd.SizeChart)
            .WithMany(sc => sc.SizeChartDetails)
            .HasForeignKey(scd => scd.SizeChartID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(scd => scd.Measurements)
            .WithOne(m => m.SizeChartDetail)
            .HasForeignKey(m => m.SizeChartDetailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
