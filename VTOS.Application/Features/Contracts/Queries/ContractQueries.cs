using VTOS.Application.Common;
using VTOS.Application.Features.Contracts.DTOs;

namespace VTOS.Application.Features.Contracts.Queries;

// ── List Contracts ──
/// <summary>
/// Used by both School (filter by SchoolId) and Provider (filter by ProviderId).
/// The handler resolves the correct scope from UserId + Role.
/// </summary>
public record GetContractsQuery(Guid UserId, string Role, string? StatusFilter);

public interface IGetContractsQueryHandler
{
    Task<Result<List<ContractDto>>> HandleAsync(GetContractsQuery query, CancellationToken ct = default);
}

// ── Contract Detail ──
public record GetContractDetailQuery(Guid UserId, string Role, Guid ContractId);

public interface IGetContractDetailQueryHandler
{
    Task<Result<ContractDto>> HandleAsync(GetContractDetailQuery query, CancellationToken ct = default);
}
