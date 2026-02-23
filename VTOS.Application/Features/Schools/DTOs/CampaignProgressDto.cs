namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// DTO for campaign pre-order progress (UC-46).
/// </summary>
public class CampaignProgressDto
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<OutfitBreakdownDto> OutfitBreakdown { get; set; } = new();
}

public class OutfitBreakdownDto
{
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public int QuantityOrdered { get; set; }
    public int? MaxQuantity { get; set; }
    public decimal Revenue { get; set; }
}
