using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetProductionOrderListQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record ProductionOrderListItemDto(
    Guid BatchId,
    string BatchName,
    string CampaignName,
    string ProviderName,
    string Status,
    int TotalQuantity,
    DateTime? DeliveryDeadline,
    DateTime CreatedDate
);

public record GetProductionOrderListResponse(
    IReadOnlyList<ProductionOrderListItemDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetProductionOrderListQueryHandler
{
    Task<Result<GetProductionOrderListResponse>> HandleAsync(GetProductionOrderListQuery query, CancellationToken ct = default);
}

public class GetProductionOrderListQueryHandler : IGetProductionOrderListQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProductionOrderListQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetProductionOrderListResponse>> HandleAsync(GetProductionOrderListQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<GetProductionOrderListResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolMgr.SchoolID;

        var q = _db.ProductionBatches.AsNoTracking()
            .Include(b => b.Campaign)
            .Include(b => b.Provider)
            .Where(b => b.Campaign.SchoolID == schoolId && !b.IsDeleted);

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<Domain.Enums.ProductionBatchStatus>(query.Status, true, out var statusEnum))
            q = q.Where(b => b.Status == statusEnum);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(b => b.CreatedDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new ProductionOrderListItemDto(
                b.Id, b.BatchName, b.Campaign.CampaignName,
                b.Provider.ProviderName, b.Status.ToString(),
                b.TotalQuantity, b.DeliveryDeadline, b.CreatedDate
            ))
            .ToListAsync(ct);

        return Result<GetProductionOrderListResponse>.Success(
            new GetProductionOrderListResponse(items, total, query.Page, query.PageSize));
    }
}
