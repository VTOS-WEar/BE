using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class PayoutRecordConfiguration : IEntityTypeConfiguration<PayoutRecord>
{
    public void Configure(EntityTypeBuilder<PayoutRecord> builder)
    {
        builder.ToTable("PayoutRecord");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("PayoutRecordID");

        builder.Property(p => p.ProviderID).IsRequired();
        builder.Property(p => p.OrderID).IsRequired();
        builder.Property(p => p.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.GrossAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.PlatformFeeRate).IsRequired().HasColumnType("decimal(5,4)");
        builder.Property(p => p.PlatformFeeAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.NetAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).IsRequired().HasMaxLength(50);
        builder.Property(p => p.PayoutMethod).IsRequired().HasMaxLength(50);
        builder.Property(p => p.AdminNote).HasMaxLength(500);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => p.OrderID).IsUnique();
        builder.HasIndex(p => p.ProviderID);

        builder.HasOne(p => p.Provider)
            .WithMany()
            .HasForeignKey(p => p.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
