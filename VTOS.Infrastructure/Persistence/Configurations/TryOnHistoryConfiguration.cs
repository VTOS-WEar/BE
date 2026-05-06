using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

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
            .HasMaxLength(500);

        builder.Property(toh => toh.ResultPhotoURL)
            .HasMaxLength(500);

        builder.Property(toh => toh.UploadedPhotoObjectKey)
            .HasMaxLength(1024);

        builder.Property(toh => toh.UploadedPhotoContentType)
            .HasMaxLength(100);

        builder.Property(toh => toh.ResultPhotoObjectKey)
            .HasMaxLength(1024);

        builder.Property(toh => toh.ResultPhotoContentType)
            .HasMaxLength(100);

        builder.Property(toh => toh.Status)
            .IsRequired()
            .HasDefaultValue(TryOnJobStatus.Completed);

        builder.Property(toh => toh.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(toh => toh.CompletedAt);

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

