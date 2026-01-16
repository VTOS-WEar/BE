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

        builder.Property(o => o.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.CampaignID);

        builder.Property(o => o.DeliveryMethod)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(o => o.ChildProfile)
            .WithMany(cp => cp.Orders)
            .HasForeignKey(o => o.ChildProfileID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Campaign)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CampaignID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

