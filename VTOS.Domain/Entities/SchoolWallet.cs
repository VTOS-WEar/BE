using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a school's wallet for managing funds and payments.
/// Maps to the SchoolWallet table in the database.
/// </summary>
public class SchoolWallet : BaseEntity
{
    public Guid SchoolID { get; set; }
    public decimal Balance { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public School School { get; set; } = null!;
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<WalletWithdrawalRequest> WithdrawalRequests { get; set; } = new List<WalletWithdrawalRequest>();
}
