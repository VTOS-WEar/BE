using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class BodygramScanRecordConfiguration : IEntityTypeConfiguration<BodygramScanRecord>
{
    public void Configure(EntityTypeBuilder<BodygramScanRecord> builder)
    {
        builder.ToTable("BodygramScanRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BodygramScanId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CustomScanId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.AvatarFormat)
            .HasMaxLength(50);

        builder.Property(x => x.AvatarType)
            .HasMaxLength(100);

        builder.Property(x => x.RawInputJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.RawMeasurementsJson)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.BodygramScanId)
            .IsUnique();

        builder.HasIndex(x => x.CustomScanId);

        builder.HasOne(x => x.Child)
            .WithMany(c => c.BodygramScanRecords)
            .HasForeignKey(x => x.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Measurements)
            .WithOne(m => m.ScanRecord)
            .HasForeignKey(m => m.ScanRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
