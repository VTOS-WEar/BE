using VTOS.Application.Common;
using VTOS.Application.Features.AccountRequests.DTOs;

namespace VTOS.Application.Features.AccountRequests.Commands;

// ── Submit Account Request (Public, no auth) ──
public record SubmitAccountRequestCommand(SubmitAccountRequestDto Request);

public interface ISubmitAccountRequestCommandHandler
{
    Task<Result<AccountRequestDetailDto>> HandleAsync(SubmitAccountRequestCommand command, CancellationToken ct = default);
}

// ── Create Account For Request (Admin) ──
public record CreateAccountForRequestCommand(Guid AdminUserId, Guid RequestId, CreateAccountForRequestDto Request);

public interface ICreateAccountForRequestCommandHandler
{
    Task<Result<AccountRequestDetailDto>> HandleAsync(CreateAccountForRequestCommand command, CancellationToken ct = default);
}

// ── Reject Account Request (Admin) ──
public record RejectAccountRequestCommand(Guid AdminUserId, Guid RequestId, string Reason);

public interface IRejectAccountRequestCommandHandler
{
    Task<Result<AccountRequestDetailDto>> HandleAsync(RejectAccountRequestCommand command, CancellationToken ct = default);
}
