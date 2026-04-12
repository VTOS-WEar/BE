using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Request body for PUT /api/schools/me/campaigns/{id} (edit draft campaign).
/// </summary>
public class UpdateCampaignRequest
{
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<CampaignOutfitRequestItem> Outfits { get; set; } = new();
}

/// <summary>
/// UC-45: Edit a draft campaign. Only Draft campaigns can be edited.
/// Updates name, dates, description, and outfit lineup.
/// </summary>
public record UpdateCampaignCommand(
    Guid UserId,
    Guid CampaignId,
    string CampaignName,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<CampaignOutfitInput> Outfits
);

public interface IUpdateCampaignCommandHandler
{
    Task<Result<CampaignDetailDto>> HandleAsync(UpdateCampaignCommand command, CancellationToken ct = default);
}
