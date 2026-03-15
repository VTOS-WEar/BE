using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Tracks distribution of uniforms to each parent Order.
/// Supports two methods:
///   - AtSchool: Parent picks up at school (no shipping info needed)
///   - AtHome: School ships via company with tracking + proof image
/// Both methods set Order.OrderStatus = Delivered directly.
/// </summary>
public class DistributionRecord : AuditableEntity
{
    public Guid BatchID { get; set; }
    public Guid OrderID { get; set; }
    public DateTime DistributedAt { get; set; }
    public string Method { get; set; } = string.Empty; // "AtSchool" or "AtHome"

    // AtHome shipping info
    public string? ShippingCompany { get; set; }
    public string? TrackingCode { get; set; }
    public string? ProofImageUrl { get; set; }

    public string? Note { get; set; }

    // Navigation properties
    public ProductionBatch Batch { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
