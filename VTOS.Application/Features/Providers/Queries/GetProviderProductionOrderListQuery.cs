using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderProductionOrderListQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record ProviderProductionOrderListItemDto(
    Guid BatchId,
    string BatchName,
    string CampaignName,
    string SchoolName,
    string Status,
    int TotalQuantity,
    DateTime? DeliveryDeadline,
    DateTime CreatedDate
);

public record GetProviderProductionOrderListResponse(
    IReadOnlyList<ProviderProductionOrderListItemDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetProviderProductionOrderListQueryHandler
{
    Task<Result<GetProviderProductionOrderListResponse>> HandleAsync(GetProviderProductionOrderListQuery query, CancellationToken ct = default);
}

public class GetProviderProductionOrderListQueryHandler : IGetProviderProductionOrderListQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderProductionOrderListQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetProviderProductionOrderListResponse>> HandleAsync(GetProviderProductionOrderListQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<GetProviderProductionOrderListResponse>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var providerId = providerMgr.ProviderID;

        var q = _db.ProductionBatches.AsNoTracking()
            .Include(b => b.Campaign).ThenInclude(c => c.School)
            .Where(b => b.ProviderID == providerId && !b.IsDeleted);

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<Domain.Enums.ProductionBatchStatus>(query.Status, true, out var statusEnum))
            q = q.Where(b => b.Status == statusEnum);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(b => b.CreatedDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new ProviderProductionOrderListItemDto(
                b.Id, b.BatchName, b.Campaign.CampaignName,
                b.Campaign.School.SchoolName,
                b.Status.ToString(),
                b.TotalQuantity, b.DeliveryDeadline, b.CreatedDate
            ))
            .ToListAsync(ct);

        return Result<GetProviderProductionOrderListResponse>.Success(
            new GetProviderProductionOrderListResponse(items, total, query.Page, query.PageSize));
    }
}
