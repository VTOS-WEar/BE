using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ApproveProviderRequestCommand(
    Guid ProviderId,
    string Action, // "APPROVE" or "REJECT"
    string? AdminNote = null
);

public interface IApproveProviderRequestCommandHandler
{
    Task<Result<string>> HandleAsync(
        ApproveProviderRequestCommand command,
        CancellationToken cancellationToken);
}
