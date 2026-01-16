using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariant");

        builder.HasKey(pv => pv.Id);
        builder.Property(pv => pv.Id).HasColumnName("ProductVariantID");

        builder.Property(pv => pv.OutfitID)
            .IsRequired();

        builder.Property(pv => pv.Size)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pv => pv.ColorVariant)
            .HasMaxLength(50);

        builder.Property(pv => pv.MaterialType)
            .HasMaxLength(100);

        builder.Property(pv => pv.StockQuantity)
            .IsRequired();

        builder.Property(pv => pv.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(pv => pv.SKUCode)
            .HasMaxLength(100);

        builder.Property(pv => pv.VariantImageURL)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(pv => pv.Outfit)
            .WithMany(o => o.ProductVariants)
            .HasForeignKey(pv => pv.OutfitID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

