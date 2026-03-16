using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ApproveProviderRequestCommand(
    Guid ProviderId,
    string Action, // "APPROVE" or "REJECT"
    string? RejectionReason = null,
    string? AdminNote = null
);

public interface IApproveProviderRequestCommandHandler
{
    Task<Result<ProviderApprovalResponse>> HandleAsync(
        ApproveProviderRequestCommand command,
        CancellationToken cancellationToken);
}
