using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class DeliveryRecordConfiguration : IEntityTypeConfiguration<DeliveryRecord>
{
    public void Configure(EntityTypeBuilder<DeliveryRecord> builder)
    {
        builder.ToTable("DeliveryRecord");

        builder.HasKey(dr => dr.Id);
        builder.Property(dr => dr.Id).HasColumnName("DeliveryRecordID");

        builder.Property(dr => dr.BatchID);

        builder.Property(dr => dr.Quantity);

        builder.Property(dr => dr.Note)
            .HasMaxLength(1000);

        builder.Property(dr => dr.DeliveredAt);

        builder.Property(dr => dr.IsConfirmed)
            .IsRequired();

        builder.Property(dr => dr.AcceptedQuantity);
        builder.Property(dr => dr.DefectiveQuantity);

        builder.Property(dr => dr.DefectNote)
            .HasMaxLength(2000);

        // Relationships
        builder.HasOne(dr => dr.Batch)
            .WithMany(pb => pb.DeliveryRecords)
            .HasForeignKey(dr => dr.BatchID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
