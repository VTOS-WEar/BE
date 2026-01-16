using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class Refund : AuditableEntity
{
    public Guid PaymentID { get; set; }
    public decimal RefundAmount { get; set; }
    public RefundStatus RefundStatus { get; set; }
    public string? DisputeReason { get; set; }

    // Navigation properties
    public PaymentTransaction PaymentTransaction { get; set; } = null!;
}

