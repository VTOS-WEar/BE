using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents an outfit associated with a campaign.
/// Maps to the CampaignOutfit table in the database.
/// </summary>
public class CampaignOutfit : BaseEntity
{
    public Guid CampaignID { get; set; }
    public Guid OutfitID { get; set; }
    public Guid? ProviderID { get; set; }
    public Guid? ContractID { get; set; }
    public decimal CampaignPrice { get; set; }
    public int? MaxQuantity { get; set; }

    // Navigation properties
    public Campaign Campaign { get; set; } = null!;
    public Outfit Outfit { get; set; } = null!;
    public Provider? Provider { get; set; }
    public Contract? Contract { get; set; }
}

