using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a withdrawal request from a school's wallet.
/// Maps to the WalletWithdrawalRequest table in the database.
/// </summary>
public class WalletWithdrawalRequest : BaseEntity
{
    public Guid WalletID { get; set; }
    public decimal Amount { get; set; }
    public decimal FeeRate { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Pending | Approved | Rejected | Paid
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? AdminNote { get; set; }

    // Navigation properties
    public Wallet Wallet { get; set; } = null!;
}
