using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.NotificationType)
            .IsRequired();

        builder.Property(n => n.ErrorMessage)
            .HasMaxLength(500);

        // Unique constraint: prevent duplicate notifications
        builder.HasIndex(n => new { n.UserId, n.NotificationType, n.ReferenceId })
            .IsUnique()
            .HasDatabaseName("IX_NotificationLog_User_Type_Ref");

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
