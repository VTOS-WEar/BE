using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedback");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("FeedbackID");

        builder.Property(f => f.UserID)
            .IsRequired();

        builder.Property(f => f.ProductVariantID)
            .IsRequired();

        builder.Property(f => f.CampaignID)
            .IsRequired();

        builder.Property(f => f.Rating)
            .IsRequired();

        builder.Property(f => f.Comment)
            .HasMaxLength(1000);

        builder.Property(f => f.Timestamp)
            .IsRequired();

        builder.Property(f => f.ModerationStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        // Relationships
        builder.HasOne(f => f.User)
            .WithMany(u => u.Feedbacks)
            .HasForeignKey(f => f.UserID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.ProductVariant)
            .WithMany(pv => pv.Feedbacks)
            .HasForeignKey(f => f.ProductVariantID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Campaign)
            .WithMany(c => c.Feedbacks)
            .HasForeignKey(f => f.CampaignID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

