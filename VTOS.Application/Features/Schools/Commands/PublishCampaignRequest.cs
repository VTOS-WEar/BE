namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Request body for POST /api/schools/me/campaigns (UC-44).
/// </summary>
public class PublishCampaignRequest
{
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool SaveAsDraft { get; set; } = false;
    public List<CampaignOutfitRequestItem> Outfits { get; set; } = new();
}

/// <summary>
/// An outfit item in the campaign request body.
/// ProviderId is optional — school can assign provider later.
/// </summary>
public class CampaignOutfitRequestItem
{
    public Guid OutfitId { get; set; }
    public Guid? ProviderId { get; set; }
    public decimal CampaignPrice { get; set; }
    public int? MaxQuantity { get; set; }
}
