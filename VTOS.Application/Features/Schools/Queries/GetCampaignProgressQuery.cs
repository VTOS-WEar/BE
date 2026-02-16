using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-46: Track pre-order progress for a campaign.
/// </summary>
public record GetCampaignProgressQuery(Guid SchoolId, Guid CampaignId);

public interface IGetCampaignProgressQueryHandler
{
    Task<Result<CampaignProgressDto>> HandleAsync(GetCampaignProgressQuery query, CancellationToken ct = default);
}
