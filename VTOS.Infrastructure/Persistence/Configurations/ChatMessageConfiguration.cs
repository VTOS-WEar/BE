using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("ChatMessageID");

        builder.Property(m => m.ChannelType)
            .IsRequired();

        builder.Property(m => m.ChannelId)
            .IsRequired();

        builder.Property(m => m.SenderUserId)
            .IsRequired();

        builder.Property(m => m.SenderName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(m => m.SentAt)
            .IsRequired();

        // Index for fast lookups: all messages in a channel, ordered by time
        builder.HasIndex(m => new { m.ChannelType, m.ChannelId, m.SentAt });

        // Relationship
        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
