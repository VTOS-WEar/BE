using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ProviderRatingConfiguration : IEntityTypeConfiguration<ProviderRating>
{
    public void Configure(EntityTypeBuilder<ProviderRating> builder)
    {
        builder.ToTable("ProviderRating");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("ProviderRatingID");

        builder.Property(x => x.ProviderID).IsRequired();
        builder.Property(x => x.OrderID).IsRequired();
        builder.Property(x => x.ParentUserID).IsRequired();
        builder.Property(x => x.Rating).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.OrderID, x.ParentUserID }).IsUnique();
        builder.HasIndex(x => x.ProviderID);

        builder.HasOne(x => x.Provider)
            .WithMany(x => x.ProviderRatings)
            .HasForeignKey(x => x.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.ProviderRatings)
            .HasForeignKey(x => x.OrderID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentUser)
            .WithMany(x => x.ProviderRatings)
            .HasForeignKey(x => x.ParentUserID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
