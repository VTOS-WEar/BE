using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a provider/supplier in the system.
/// Maps to the Provider table in the database.
/// </summary>
public class Provider : BaseEntity
{
    public string ProviderName { get; set; } = string.Empty;
    public string? ContactPersonName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Status { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public ICollection<CampaignOutfit> CampaignOutfits { get; set; } = new List<CampaignOutfit>();
    public ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
