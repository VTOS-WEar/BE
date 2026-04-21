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
    public Guid? ProviderID { get; set; }
    public Guid? SemesterPublicationID { get; set; }
    public string? DeliveryMethod { get; set; }
    public string? TrackingCode { get; set; }
    public string? ShippingCompany { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? CancelReason { get; set; }
    public bool IsProviderPaid { get; set; }

    // Navigation properties
    public ChildProfile ChildProfile { get; set; } = null!;
    public Campaign? Campaign { get; set; }
    public Provider? Provider { get; set; }
    public SemesterPublication? SemesterPublication { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<ProviderRating> ProviderRatings { get; set; } = new List<ProviderRating>();
}

