using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refund");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("RefundID");

        builder.Property(r => r.PaymentID)
            .IsRequired();

        builder.Property(r => r.RefundAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.RefundStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(r => r.DisputeReason)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(r => r.PaymentTransaction)
            .WithMany(pt => pt.Refunds)
            .HasForeignKey(r => r.PaymentID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

