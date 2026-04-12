using VTOS.Application.Common;
using VTOS.Application.Features.Contracts.DTOs;

namespace VTOS.Application.Features.Contracts.Commands;

// ── Create Contract (School) ──────────────────────────────────────────────────
public record CreateContractCommand(Guid UserId, CreateContractRequest Request);

public interface ICreateContractCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(CreateContractCommand command, CancellationToken ct = default);
}

// ── Approve Contract (Provider) — moves to PendingSchoolSign ─────────────────
public record ApproveContractCommand(Guid UserId, Guid ContractId);

public interface IApproveContractCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(ApproveContractCommand command, CancellationToken ct = default);
}

// ── Reject Contract (Provider) ────────────────────────────────────────────────
public record RejectContractCommand(Guid UserId, Guid ContractId, string Reason);

public interface IRejectContractCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(RejectContractCommand command, CancellationToken ct = default);
}

// ── Cancel Contract (School) — allowed at Pending or PendingSchoolSign ────────
public record CancelContractCommand(Guid UserId, Guid ContractId);

public interface ICancelContractCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(CancelContractCommand command, CancellationToken ct = default);
}

// ── Request Sign OTP (School or Provider) ────────────────────────────────────
/// <summary>
/// Generates a 6-digit OTP and sends it to the requesting user's email.
/// Role: "School" (for PendingSchoolSign) or "Provider" (for PendingProviderSign).
/// </summary>
public record RequestSignOTPCommand(Guid UserId, Guid ContractId, string Role);

public interface IRequestSignOTPCommandHandler
{
    Task<Result<bool>> HandleAsync(RequestSignOTPCommand command, CancellationToken ct = default);
}

// ── Sign Contract by School ───────────────────────────────────────────────────
/// <summary>
/// Validates OTP, stores signature, transitions Pending SchoolSign → PendingProviderSign.
/// </summary>
public record SignContractBySchoolCommand(Guid UserId, Guid ContractId, SignContractRequest Request);

public interface ISignContractBySchoolCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(SignContractBySchoolCommand command, CancellationToken ct = default);
}

// ── Sign Contract by Provider ─────────────────────────────────────────────────
/// <summary>
/// Validates OTP, stores signature, transitions PendingProviderSign → Active.
/// </summary>
public record SignContractByProviderCommand(Guid UserId, Guid ContractId, SignContractRequest Request);

public interface ISignContractByProviderCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(SignContractByProviderCommand command, CancellationToken ct = default);
}
