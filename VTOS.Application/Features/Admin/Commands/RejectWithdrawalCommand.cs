using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Commands;

namespace VTOS.Application.Features.Admin.Commands;

public record RejectWithdrawalCommand(Guid WithdrawalRequestId, string? AdminNote);

public interface IRejectWithdrawalCommandHandler
{
    Task<Result<WithdrawalRequestResponse>> HandleAsync(RejectWithdrawalCommand command, CancellationToken ct = default);
}
