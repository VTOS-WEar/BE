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
            .HasConversion<string>();

        builder.Property(p => p.VerificationStatus)
            .HasConversion<string>();

        builder.Property(p => p.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(p => p.VerificationDocumentUrl)
            .HasMaxLength(500);

        builder.Property(p => p.AverageRating)
            .HasColumnType("decimal(4,2)")
            .HasDefaultValue(0m);

        builder.Property(p => p.TotalRatings)
            .HasDefaultValue(0);

        builder.Property(p => p.TotalCompletedOrders)
            .HasDefaultValue(0);

        builder.Property(p => p.IsDeleted)
            .IsRequired();

        builder.HasIndex(p => p.IsDeleted);
    }
}
