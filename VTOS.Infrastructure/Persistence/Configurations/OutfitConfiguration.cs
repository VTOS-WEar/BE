using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class OutfitConfiguration : IEntityTypeConfiguration<Outfit>
{
    public void Configure(EntityTypeBuilder<Outfit> builder)
    {
        builder.ToTable("Outfit");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("OutfitID");

        builder.Property(o => o.SchoolID)
            .IsRequired();

        builder.Property(o => o.OutfitName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(o => o.Description)
            .HasMaxLength(500);

        builder.Property(o => o.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.OutfitType)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion<string>();

        builder.Property(o => o.MainImageURL)
            .HasMaxLength(500);

        builder.Property(o => o.SizeChartID);

        builder.Property(o => o.IsAvailable)
            .IsRequired();

        builder.Property(o => o.IsCustomizable)
            .IsRequired();

        // Relationships
        builder.HasOne(o => o.School)
            .WithMany(s => s.Outfits)
            .HasForeignKey(o => o.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.SizeChart)
            .WithMany(sc => sc.Outfits)
            .HasForeignKey(o => o.SizeChartID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

