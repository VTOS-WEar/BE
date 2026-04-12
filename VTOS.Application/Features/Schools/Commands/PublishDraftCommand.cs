using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-45b: Publish a draft campaign, making it visible and open for parent orders.
/// Only Draft campaigns can be published via this endpoint.
/// Validates date range, outfit availability, and provider contracts before activation.
/// </summary>
public record PublishDraftCommand(Guid UserId, Guid CampaignId);

public interface IPublishDraftCommandHandler
{
    Task<Result<PublishCampaignResponseDto>> HandleAsync(PublishDraftCommand command, CancellationToken ct = default);
}
