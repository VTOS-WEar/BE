using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a production batch for a campaign.
/// Maps to the ProductionBatch table in the database.
/// </summary>
public class ProductionBatch : BaseEntity
{
    public Guid CampaignID { get; set; }
    public Guid ProviderID { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? Status { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Campaign Campaign { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
}
