namespace VTOS.Application.Common.Models;

public class RefundResponse
{
    public Guid RefundId { get; set; }
    public Guid OrderId { get; set; }
    public Guid PaymentTransactionId { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundStatus { get; set; } = string.Empty;
    public string? DisputeReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
