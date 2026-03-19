using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>UC 3.9.14 — Get the uniform items within a production order.</summary>
public record GetProductionOrderItemsQuery(Guid UserId, Guid BatchId);

public interface IGetProductionOrderItemsQueryHandler
{
    Task<Result<IReadOnlyList<ProductionBatchItemDto>>> HandleAsync(GetProductionOrderItemsQuery query, CancellationToken ct = default);
}

public class GetProductionOrderItemsQueryHandler : IGetProductionOrderItemsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProductionOrderItemsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProductionBatchItemDto>>> HandleAsync(GetProductionOrderItemsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<IReadOnlyList<ProductionBatchItemDto>>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batchExists = await _db.ProductionBatches.AsNoTracking()
            .AnyAsync(b => b.Id == query.BatchId && b.Campaign.SchoolID == schoolMgr.SchoolID && !b.IsDeleted, ct);
        if (!batchExists)
            return Result<IReadOnlyList<ProductionBatchItemDto>>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        var items = await _db.ProductionBatchItems
            .AsNoTracking()
            .Include(i => i.Outfit)
            .Where(i => i.BatchID == query.BatchId)
            .OrderBy(i => i.Outfit.OutfitName).ThenBy(i => i.Size)
            .Select(i => new ProductionBatchItemDto(i.Id, i.OutfitID, i.Outfit.OutfitName, i.Size, i.Quantity, i.UnitPrice))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProductionBatchItemDto>>.Success(items);
    }
}
