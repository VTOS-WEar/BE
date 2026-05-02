using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class PayoutRecord : BaseEntity
{
    public Guid ProviderID { get; set; }
    public Guid OrderID { get; set; }
    public decimal Amount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal PlatformFeeRate { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = "Completed";
    public string PayoutMethod { get; set; } = "SystemCredits";
    public DateTime? ProcessedAt { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Provider Provider { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
