using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Commands;

namespace VTOS.Application.Features.Admin.Commands;

public record ApproveWithdrawalCommand(Guid WithdrawalRequestId, string TransferProofImageUrl, string? AdminNote);

public interface IApproveWithdrawalCommandHandler
{
    Task<Result<WithdrawalRequestResponse>> HandleAsync(ApproveWithdrawalCommand command, CancellationToken ct = default);
}
