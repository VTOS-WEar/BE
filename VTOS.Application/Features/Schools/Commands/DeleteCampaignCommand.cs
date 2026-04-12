using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-45c: Delete a draft campaign.
/// Only Draft campaigns with no orders can be deleted.
/// Active/Completed/Locked campaigns are protected.
/// </summary>
public record DeleteCampaignCommand(Guid UserId, Guid CampaignId);

public interface IDeleteCampaignCommandHandler
{
    Task<Result<string>> HandleAsync(DeleteCampaignCommand command, CancellationToken ct = default);
}
