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
    /// <summary>Number of distinct students (ChildProfile) who placed orders in this campaign.</summary>
    public int TotalStudents { get; set; }
    /// <summary>Orders with status Pending or Paid (awaiting confirmation).</summary>
    public int PendingOrders { get; set; }
    /// <summary>Total students in the school (for X/Y progress display).</summary>
    public int TotalChildProfiles { get; set; }
    public List<OutfitBreakdownDto> OutfitBreakdown { get; set; } = new();
}

public class OutfitBreakdownDto
{
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public int QuantityOrdered { get; set; }
    public int? MaxQuantity { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Outfit category/type for product classification display.</summary>
    public string? Category { get; set; }
}
