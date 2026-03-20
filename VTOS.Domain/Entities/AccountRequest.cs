using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a partnership request from a School or Provider.
/// Admin reviews and manually creates accounts instead of self-registration.
/// </summary>
public class AccountRequest : BaseEntity
{
    public string OrganizationName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public AccountRequestType Type { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public AccountRequestStatus Status { get; set; } = AccountRequestStatus.Pending;
    public string? RejectionReason { get; set; }

    /// <summary>Admin user who processed this request</summary>
    public Guid? ProcessedByUserId { get; set; }

    /// <summary>The User account created for this request (after approval)</summary>
    public Guid? CreatedUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public User? ProcessedByUser { get; set; }
    public User? CreatedUser { get; set; }
}
