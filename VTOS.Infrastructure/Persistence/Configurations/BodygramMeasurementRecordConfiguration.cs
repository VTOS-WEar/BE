using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class BodygramMeasurementRecordConfiguration : IEntityTypeConfiguration<BodygramMeasurementRecord>
{
    public void Configure(EntityTypeBuilder<BodygramMeasurementRecord> builder)
    {
        builder.ToTable("BodygramMeasurementRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => new { x.ScanRecordId, x.Name });
    }
}
