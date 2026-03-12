using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class ParentBankAccount : AuditableEntity
{
    public Guid ParentUserID { get; set; }

    public string BankName { get; set; } = string.Empty;
    public string? BankCode { get; set; }

    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
    public bool IsVerified { get; set; }

    // Navigation properties
    public User ParentUser { get; set; } = null!;
}
