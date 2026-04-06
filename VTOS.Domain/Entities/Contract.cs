using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Contract between a School and a Provider.
/// Defines pricing and quantity terms for outfits BEFORE campaigns.
/// </summary>
public class Contract : BaseEntity
{
    public Guid SchoolID { get; set; }
    public Guid ProviderID { get; set; }
    public string ContractName { get; set; } = string.Empty;

    /// <summary>Pending | Approved | InUse | Fulfilled | Rejected | Expired</summary>
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Contract expiration date. Required — protects Provider if School doesn't open a campaign.</summary>
    public DateTime ExpiresAt { get; set; }

    // Navigation
    public School School { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public ICollection<ContractItem> ContractItems { get; set; } = new List<ContractItem>();
}

