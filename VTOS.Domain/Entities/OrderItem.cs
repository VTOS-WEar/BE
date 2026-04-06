using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class OrderItem : AuditableEntity
{
    public Guid OrderID { get; set; }
    public Guid ProductVariantID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string SizeOrdered { get; set; } = string.Empty;
    public bool IsCustomOrder { get; set; }
    public string? CustomMeasurements { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}

