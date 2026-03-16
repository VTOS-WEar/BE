using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Provider");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("ProviderID");

        builder.Property(p => p.ProviderName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.ContactPersonName)
            .HasMaxLength(255);

        builder.Property(p => p.Phone)
            .HasMaxLength(30);

        builder.Property(p => p.Email)
            .HasMaxLength(255);

        builder.Property(p => p.Address)
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .HasDefaultValue(VTOS.Domain.Enums.ProviderStatus.Pending)
            .HasConversion<string>();

        builder.Property(p => p.VerificationStatus)
            .HasDefaultValue(VTOS.Domain.Enums.VerificationStatus.Pending)
            .HasConversion<string>();

        builder.Property(p => p.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(p => p.VerificationDocumentUrl)
            .HasMaxLength(500);

        builder.Property(p => p.IsDeleted)
            .IsRequired();

        builder.HasIndex(p => p.IsDeleted);

        // Relationships
        builder.HasMany(p => p.CampaignOutfits)
            .WithOne(co => co.Provider)
            .HasForeignKey(co => co.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.ProductionBatches)
            .WithOne(pb => pb.Provider)
            .HasForeignKey(pb => pb.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
