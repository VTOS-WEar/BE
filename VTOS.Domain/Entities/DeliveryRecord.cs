using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Tracks each partial delivery shipment from Provider to School.
/// A ProductionBatch can have multiple DeliveryRecords (partial deliveries).
/// Total delivered must equal ProductionBatch.TotalQuantity (100%) to mark batch as Delivered.
/// </summary>
public class DeliveryRecord : AuditableEntity
{
    public Guid BatchID { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public DateTime DeliveredAt { get; set; }

    // School confirmation
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int? AcceptedQuantity { get; set; }
    public int? DefectiveQuantity { get; set; }
    public string? DefectNote { get; set; }

    // Navigation properties
    public ProductionBatch Batch { get; set; } = null!;
}
