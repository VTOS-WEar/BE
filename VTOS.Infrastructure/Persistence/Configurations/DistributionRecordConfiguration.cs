using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class DistributionRecordConfiguration : IEntityTypeConfiguration<DistributionRecord>
{
    public void Configure(EntityTypeBuilder<DistributionRecord> builder)
    {
        builder.ToTable("DistributionRecord");

        builder.HasKey(dr => dr.Id);
        builder.Property(dr => dr.Id).HasColumnName("DistributionRecordID");

        builder.Property(dr => dr.BatchID);
        builder.Property(dr => dr.OrderID);

        builder.Property(dr => dr.DistributedAt);

        builder.Property(dr => dr.Method)
            .IsRequired()
            .HasMaxLength(20); // "AtSchool" or "AtHome"

        builder.Property(dr => dr.ShippingCompany)
            .HasMaxLength(100);

        builder.Property(dr => dr.TrackingCode)
            .HasMaxLength(100);

        builder.Property(dr => dr.ProofImageUrl)
            .HasMaxLength(500);

        builder.Property(dr => dr.Note)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(dr => dr.Batch)
            .WithMany(pb => pb.DistributionRecords)
            .HasForeignKey(dr => dr.BatchID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dr => dr.Order)
            .WithMany()
            .HasForeignKey(dr => dr.OrderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
