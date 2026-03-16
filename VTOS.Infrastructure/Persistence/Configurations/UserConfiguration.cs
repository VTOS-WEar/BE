using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("UserID");

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(u => u.Phone)
            .HasMaxLength(30);
            
        builder.Property(u => u.DOB)
            .HasColumnType("date");

        builder.Property(u => u.Gender)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.Avatar)
            .HasMaxLength(500);

        builder.Property(u => u.RoleID)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.IsDeleted)
            .IsRequired();

        builder.HasIndex(u => u.IsDeleted);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastLogin);

        // Password Reset Token (SHA-256 hash = 64 chars)
        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(64);

        builder.Property(u => u.PasswordResetTokenExpiry);

        // Relationships
        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ChildProfiles)
            .WithOne(cp => cp.ParentUser)
            .HasForeignKey(cp => cp.ParentUserID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Provider)
            .WithMany()
            .HasForeignKey(u => u.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
