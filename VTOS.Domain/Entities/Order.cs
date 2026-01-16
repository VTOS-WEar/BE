using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class Order : AuditableEntity
{
    public Guid ChildProfileID { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public Guid? CampaignID { get; set; }
    public string? DeliveryMethod { get; set; }

    // Navigation properties
    public ChildProfile ChildProfile { get; set; } = null!;
    public Campaign? Campaign { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

