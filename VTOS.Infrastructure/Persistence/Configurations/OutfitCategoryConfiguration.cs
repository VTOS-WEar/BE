using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class OutfitCategoryConfiguration : IEntityTypeConfiguration<OutfitCategory>
{
    public void Configure(EntityTypeBuilder<OutfitCategory> builder)
    {
        builder.ToTable("OutfitCategory");

        builder.HasKey(oc => new { oc.OutfitID, oc.CategoryID });

        builder.Property(oc => oc.OutfitID)
            .HasColumnName("OutfitID");

        builder.Property(oc => oc.CategoryID)
            .HasColumnName("CategoryID");

        // Relationships
        builder.HasOne(oc => oc.Outfit)
            .WithMany(o => o.OutfitCategories)
            .HasForeignKey(oc => oc.OutfitID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oc => oc.Category)
            .WithMany(c => c.OutfitCategories)
            .HasForeignKey(oc => oc.CategoryID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

