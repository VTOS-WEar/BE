using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Commands;

namespace VTOS.Application.Features.Providers.Commands;

public record CreateProviderWithdrawalRequestCommand(Guid ProviderUserId, decimal Amount);

public interface ICreateProviderWithdrawalRequestCommandHandler
{
    Task<Result<WithdrawalRequestResponse>> HandleAsync(CreateProviderWithdrawalRequestCommand command, CancellationToken ct = default);
}
