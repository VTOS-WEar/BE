using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a line item within a production batch.
/// Tracks which outfit variant, size, and quantity must be produced.
/// Created when a school generates a production order (UC 3.9.8).
/// </summary>
public class ProductionBatchItem : BaseEntity
{
    public Guid BatchID { get; set; }
    public Guid OutfitID { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navigation properties
    public ProductionBatch Batch { get; set; } = null!;
    public Outfit Outfit { get; set; } = null!;
}
