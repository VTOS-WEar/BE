using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Generic chat message entity used across multiple channels (Complaints, Contracts).
/// Supports real-time messaging via SignalR.
/// </summary>
public class ChatMessage : BaseEntity
{
    public ChatChannelType ChannelType { get; set; }
    public Guid ChannelId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    // Navigation
    public User Sender { get; set; } = null!;
}
