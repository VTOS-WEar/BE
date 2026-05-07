using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Order");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("OrderID");

        builder.Property(o => o.ChildProfileID)
            .IsRequired();

        builder.Property(o => o.OrderDate)
            .IsRequired();

        builder.Property(o => o.OrderStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.ShippingFee)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.ProviderID);
        builder.Property(o => o.SemesterPublicationID);
        builder.Property(o => o.AppliedPricingMode)
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(o => o.DeliveryMethod)
            .HasMaxLength(50);

        builder.Property(o => o.TrackingCode)
            .HasMaxLength(100);

        builder.Property(o => o.ShippingCompany)
            .HasMaxLength(100);

        builder.Property(o => o.RecipientName)
            .HasMaxLength(255);

        builder.Property(o => o.RecipientPhone)
            .HasMaxLength(30);

        builder.Property(o => o.CancelReason)
            .HasMaxLength(500);

        builder.HasIndex(o => o.ProviderID);
        builder.HasIndex(o => o.SemesterPublicationID);

        // Relationships
        builder.HasOne(o => o.ChildProfile)
            .WithMany(cp => cp.Orders)
            .HasForeignKey(o => o.ChildProfileID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Provider)
            .WithMany(p => p.Orders)
            .HasForeignKey(o => o.ProviderID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.SemesterPublication)
            .WithMany(sp => sp.Orders)
            .HasForeignKey(o => o.SemesterPublicationID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

