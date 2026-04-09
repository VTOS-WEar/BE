using VTOS.Application.Common;
using VTOS.Application.Features.Contracts.DTOs;

namespace VTOS.Application.Features.Contracts.Commands;

// ── Create Contract (School) ──
public record CreateContractCommand(Guid UserId, CreateContractRequest Request);

public interface ICreateContractCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(CreateContractCommand command, CancellationToken ct = default);
}

// ── Approve Contract (Provider) ──
public record ApproveContractCommand(Guid UserId, Guid ContractId);

public interface IApproveContractCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(ApproveContractCommand command, CancellationToken ct = default);
}

// ── Reject Contract (Provider) ──
public record RejectContractCommand(Guid UserId, Guid ContractId, string Reason);

public interface IRejectContractCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(RejectContractCommand command, CancellationToken ct = default);
}

// ── Cancel Contract (School) ──
public record CancelContractCommand(Guid UserId, Guid ContractId);

public interface ICancelContractCommandHandler
{
    Task<Result<ContractDto>> HandleAsync(CancelContractCommand command, CancellationToken ct = default);
}
