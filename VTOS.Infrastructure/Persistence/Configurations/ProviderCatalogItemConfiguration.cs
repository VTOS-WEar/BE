using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ProviderCatalogItemConfiguration : IEntityTypeConfiguration<ProviderCatalogItem>
{
    public void Configure(EntityTypeBuilder<ProviderCatalogItem> builder)
    {
        builder.ToTable("ProviderCatalogItem");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("ProviderCatalogItemID");

        builder.Property(x => x.ProviderID).IsRequired();
        builder.Property(x => x.ContractItemID).IsRequired();
        builder.Property(x => x.OutfitID).IsRequired();
        builder.Property(x => x.SemesterPublicationProviderID).IsRequired();

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ShortDescription)
            .HasMaxLength(500);

        builder.Property(x => x.FullDescription)
            .HasMaxLength(4000);

        builder.Property(x => x.MaterialDetails)
            .HasMaxLength(2000);

        builder.Property(x => x.CareInstructions)
            .HasMaxLength(2000);

        builder.Property(x => x.MainImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.GalleryImageUrls)
            .HasMaxLength(4000);

        builder.Property(x => x.PublicationPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.PostDeadlinePrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.HasIndex(x => x.ProviderID);
        builder.HasIndex(x => x.OutfitID);
        builder.HasIndex(x => new { x.SemesterPublicationProviderID, x.ContractItemID })
            .IsUnique();

        builder.HasOne(x => x.Provider)
            .WithMany(x => x.ProviderCatalogItems)
            .HasForeignKey(x => x.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ContractItem)
            .WithMany(x => x.ProviderCatalogItems)
            .HasForeignKey(x => x.ContractItemID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Outfit)
            .WithMany(x => x.ProviderCatalogItems)
            .HasForeignKey(x => x.OutfitID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SemesterPublicationProvider)
            .WithMany(x => x.ProviderCatalogItems)
            .HasForeignKey(x => x.SemesterPublicationProviderID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
