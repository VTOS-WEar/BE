using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItem");

        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Id).HasColumnName("OrderItemID");

        builder.Property(oi => oi.OrderID)
            .IsRequired();

        builder.Property(oi => oi.ProductVariantID)
            .IsRequired();

        builder.Property(oi => oi.Quantity)
            .IsRequired();

        builder.Property(oi => oi.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.SizeOrdered)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(oi => oi.IsCustomOrder)
            .IsRequired();

        builder.Property(oi => oi.CustomMeasurements)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.ProductVariant)
            .WithMany(pv => pv.OrderItems)
            .HasForeignKey(oi => oi.ProductVariantID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

