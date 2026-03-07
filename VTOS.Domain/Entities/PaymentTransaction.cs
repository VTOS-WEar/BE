using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class PaymentTransaction : AuditableEntity
{
    public Guid OrderID { get; set; }
    public Guid? WalletID { get; set; }
    public string PaymentLinkId { get; set; }

    public PaymentGatewayType GatewayType { get; set; }
    public PaymentStatus TransactionStatus { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionTimestamp { get; set; }
    public string? TransactionLog { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public SchoolWallet? Wallet { get; set; }
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}

