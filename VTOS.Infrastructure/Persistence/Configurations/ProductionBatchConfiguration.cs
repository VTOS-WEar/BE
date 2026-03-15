using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ProductionBatchConfiguration : IEntityTypeConfiguration<ProductionBatch>
{
    public void Configure(EntityTypeBuilder<ProductionBatch> builder)
    {
        builder.ToTable("ProductionBatch");

        builder.HasKey(pb => pb.Id);
        builder.Property(pb => pb.Id).HasColumnName("BatchID");

        builder.Property(pb => pb.CampaignID);

        builder.Property(pb => pb.ProviderID);

        builder.Property(pb => pb.BatchName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pb => pb.TotalQuantity);

        builder.Property(pb => pb.CreatedDate);

        builder.Property(pb => pb.Status)
            .HasMaxLength(20);

        builder.Property(pb => pb.IsDeleted)
            .IsRequired();

        builder.HasIndex(pb => pb.IsDeleted);

        // Phase 4 — Delivery tracking
        builder.Property(pb => pb.DeliveredQuantity);
        builder.Property(pb => pb.DeliveryNote).HasMaxLength(1000);

        // Relationships
        builder.HasOne(pb => pb.Campaign)
            .WithMany(c => c.ProductionBatches)
            .HasForeignKey(pb => pb.CampaignID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pb => pb.Provider)
            .WithMany(p => p.ProductionBatches)
            .HasForeignKey(pb => pb.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
