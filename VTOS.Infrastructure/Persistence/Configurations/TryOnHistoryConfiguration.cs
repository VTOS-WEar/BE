using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class TryOnHistoryConfiguration : IEntityTypeConfiguration<TryOnHistory>
{
    public void Configure(EntityTypeBuilder<TryOnHistory> builder)
    {
        builder.ToTable("TryOnHistory");

        builder.HasKey(toh => toh.Id);
        builder.Property(toh => toh.Id).HasColumnName("TryOnID");

        builder.Property(toh => toh.GuestSessionID)
            .HasMaxLength(100);

        builder.Property(toh => toh.UserID);

        builder.Property(toh => toh.ChildID);

        builder.Property(toh => toh.OutfitID)
            .IsRequired();

        builder.Property(toh => toh.UploadedPhotoURL)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(toh => toh.ResultPhotoURL)
            .HasMaxLength(500);

        builder.Property(toh => toh.TryOnTimestamp)
            .IsRequired();

        builder.Property(toh => toh.AlignmentAdjustment)
            .HasMaxLength(500);

        builder.Property(toh => toh.SourcePlatform)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(toh => toh.User)
            .WithMany(u => u.TryOnHistories)
            .HasForeignKey(toh => toh.UserID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(toh => toh.ChildProfile)
            .WithMany(cp => cp.TryOnHistories)
            .HasForeignKey(toh => toh.ChildID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(toh => toh.Outfit)
            .WithMany(o => o.TryOnHistories)
            .HasForeignKey(toh => toh.OutfitID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

