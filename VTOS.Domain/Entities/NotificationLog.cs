using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Tracks sent email notifications to prevent duplicates.
/// Unique constraint on (UserId, NotificationType, ReferenceId).
/// </summary>
public class NotificationLog : BaseEntity
{
    public Guid UserId { get; set; }
    public NotificationType NotificationType { get; set; }
    public Guid ReferenceId { get; set; }  // Generic FK: OrderID, CampaignID, ContractID
    public string Email { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
