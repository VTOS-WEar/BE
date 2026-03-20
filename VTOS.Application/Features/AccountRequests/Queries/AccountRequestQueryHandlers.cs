using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.AccountRequests.Commands;
using VTOS.Application.Features.AccountRequests.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.AccountRequests.Queries;

// ─── Get Account Requests List Handler (Admin) ───
public class GetAccountRequestsQueryHandler : IGetAccountRequestsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAccountRequestsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<AccountRequestListResponse>> HandleAsync(
        GetAccountRequestsQuery query, CancellationToken ct = default)
    {
        var q = _context.AccountRequests.AsNoTracking().AsQueryable();

        // Filter by status
        if (query.Status.HasValue && Enum.IsDefined(typeof(AccountRequestStatus), query.Status.Value))
            q = q.Where(ar => ar.Status == (AccountRequestStatus)query.Status.Value);

        // Filter by type
        if (query.Type.HasValue && Enum.IsDefined(typeof(AccountRequestType), query.Type.Value))
            q = q.Where(ar => ar.Type == (AccountRequestType)query.Type.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(ar => ar.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(AccountRequestMapper.MapToListDto).ToList();

        return Result<AccountRequestListResponse>.Success(
            new AccountRequestListResponse(dtos, totalCount, query.Page, query.PageSize));
    }
}

// ─── Get Account Request Detail Handler (Admin) ───
public class GetAccountRequestDetailQueryHandler : IGetAccountRequestDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAccountRequestDetailQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<AccountRequestDetailDto>> HandleAsync(
        GetAccountRequestDetailQuery query, CancellationToken ct = default)
    {
        var accountRequest = await _context.AccountRequests
            .AsNoTracking()
            .Include(ar => ar.ProcessedByUser)
            .Include(ar => ar.CreatedUser)
            .FirstOrDefaultAsync(ar => ar.Id == query.RequestId, ct);

        if (accountRequest == null)
            return Result<AccountRequestDetailDto>.Failure("Yêu cầu không tồn tại.", "NOT_FOUND");

        return Result<AccountRequestDetailDto>.Success(AccountRequestMapper.MapToDetailDto(accountRequest));
    }
}
