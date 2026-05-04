using VTOS.Application.Common;
using VTOS.Application.Features.Account.DTOs;

namespace VTOS.Application.Features.Account.Commands;

public record UpdateAccountEmailCommand(Guid UserId, string Email);

public interface IUpdateAccountEmailCommandHandler
{
    Task<Result<UpdateAccountEmailResponse>> HandleAsync(
        UpdateAccountEmailCommand command,
        CancellationToken ct = default);
}
