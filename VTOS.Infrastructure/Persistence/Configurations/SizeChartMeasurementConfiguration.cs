using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SizeChartMeasurementConfiguration : IEntityTypeConfiguration<SizeChartMeasurement>
{
    public void Configure(EntityTypeBuilder<SizeChartMeasurement> builder)
    {
        builder.ToTable("SizeChartMeasurement");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("MeasurementID");

        builder.Property(m => m.SizeChartDetailId)
            .IsRequired();

        builder.Property(m => m.FieldKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Unit)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("cm");

        builder.Property(m => m.MinCm)
            .HasColumnType("decimal(6,2)");

        builder.Property(m => m.MaxCm)
            .HasColumnType("decimal(6,2)");
    }
}
