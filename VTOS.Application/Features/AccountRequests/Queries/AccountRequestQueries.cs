using VTOS.Application.Common;
using VTOS.Application.Features.AccountRequests.DTOs;

namespace VTOS.Application.Features.AccountRequests.Queries;

// ── Get Account Requests (Admin) ──
public record GetAccountRequestsQuery(int Page = 1, int PageSize = 20, int? Status = null, int? Type = null);

public interface IGetAccountRequestsQueryHandler
{
    Task<Result<AccountRequestListResponse>> HandleAsync(GetAccountRequestsQuery query, CancellationToken ct = default);
}

// ── Get Account Request Detail (Admin) ──
public record GetAccountRequestDetailQuery(Guid RequestId);

public interface IGetAccountRequestDetailQueryHandler
{
    Task<Result<AccountRequestDetailDto>> HandleAsync(GetAccountRequestDetailQuery query, CancellationToken ct = default);
}
