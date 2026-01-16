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

        builder.Property(scd => scd.ChestMin)
            .HasColumnType("decimal(5,2)");

        builder.Property(scd => scd.ChestMax)
            .HasColumnType("decimal(5,2)");

        builder.Property(scd => scd.WaistMin)
            .HasColumnType("decimal(5,2)");

        builder.Property(scd => scd.WaistMax)
            .HasColumnType("decimal(5,2)");

        builder.Property(scd => scd.HipMin)
            .HasColumnType("decimal(5,2)");

        builder.Property(scd => scd.HipMax)
            .HasColumnType("decimal(5,2)");

        builder.Property(scd => scd.HeightMin)
            .HasColumnType("decimal(5,2)");

        builder.Property(scd => scd.HeightMax)
            .HasColumnType("decimal(5,2)");

        builder.Property(scd => scd.OtherMeasurements)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(scd => scd.SizeChart)
            .WithMany(sc => sc.SizeChartDetails)
            .HasForeignKey(scd => scd.SizeChartID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

