using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a wallet for managing funds and payments.
/// Can belong to either a School or a Provider (determined by OwnerType).
/// Maps to the Wallets table in the database.
/// </summary>
public class Wallet : BaseEntity
{
    public Guid OwnerID { get; set; }
    public WalletOwnerType OwnerType { get; set; }
    public decimal Balance { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public School? School { get; set; }
    public Provider? Provider { get; set; }
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<WalletWithdrawalRequest> WithdrawalRequests { get; set; } = new List<WalletWithdrawalRequest>();
}
