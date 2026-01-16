using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class CampaignOutfitConfiguration : IEntityTypeConfiguration<CampaignOutfit>
{
    public void Configure(EntityTypeBuilder<CampaignOutfit> builder)
    {
        builder.ToTable("CampaignOutfit");

        builder.HasKey(co => co.Id);
        builder.Property(co => co.Id).HasColumnName("CampaignOutfitID");

        builder.Property(co => co.CampaignID);

        builder.Property(co => co.OutfitID);

        builder.Property(co => co.ProviderID);

        builder.Property(co => co.CampaignPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(co => co.MaxQuantity);

        // Relationships
        builder.HasOne(co => co.Campaign)
            .WithMany(c => c.CampaignOutfits)
            .HasForeignKey(co => co.CampaignID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(co => co.Outfit)
            .WithMany(o => o.CampaignOutfits)
            .HasForeignKey(co => co.OutfitID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(co => co.Provider)
            .WithMany(p => p.CampaignOutfits)
            .HasForeignKey(co => co.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
