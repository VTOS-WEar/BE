using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Commands;

namespace VTOS.Application.Features.Users.Commands;

public record CreateParentWithdrawalRequestCommand(Guid ParentUserId, decimal Amount);

public interface ICreateParentWithdrawalRequestCommandHandler
{
    Task<Result<WithdrawalRequestResponse>> HandleAsync(CreateParentWithdrawalRequestCommand command, CancellationToken ct = default);
}
