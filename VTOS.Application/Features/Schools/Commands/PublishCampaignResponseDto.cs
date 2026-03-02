namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Response DTO returned after successfully publishing a campaign.
/// </summary>
public class PublishCampaignResponseDto
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int OutfitCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
