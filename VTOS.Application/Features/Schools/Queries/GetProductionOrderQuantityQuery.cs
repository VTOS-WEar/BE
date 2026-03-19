using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>UC 3.9.15 — View required quantity per outfit/size in a production order.</summary>
public record GetProductionOrderQuantityQuery(Guid UserId, Guid BatchId);

public record ProductionOutfitQuantityDto(
    Guid OutfitId,
    string OutfitName,
    IReadOnlyList<SizeQuantityDto> BySizeRequired,
    int TotalRequired
);

public record ProductionOrderQuantityDto(
    Guid BatchId,
    string BatchName,
    int GrandTotal,
    IReadOnlyList<ProductionOutfitQuantityDto> Outfits
);

public interface IGetProductionOrderQuantityQueryHandler
{
    Task<Result<ProductionOrderQuantityDto>> HandleAsync(GetProductionOrderQuantityQuery query, CancellationToken ct = default);
}

public class GetProductionOrderQuantityQueryHandler : IGetProductionOrderQuantityQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProductionOrderQuantityQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ProductionOrderQuantityDto>> HandleAsync(GetProductionOrderQuantityQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<ProductionOrderQuantityDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches.AsNoTracking()
            .Include(b => b.Items).ThenInclude(i => i.Outfit)
            .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.Campaign.SchoolID == schoolMgr.SchoolID && !b.IsDeleted, ct);

        if (batch == null)
            return Result<ProductionOrderQuantityDto>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        var outfits = batch.Items
            .GroupBy(i => new { i.OutfitID, i.Outfit.OutfitName })
            .Select(g => new ProductionOutfitQuantityDto(
                g.Key.OutfitID, g.Key.OutfitName,
                g.OrderBy(i => i.Size).Select(i => new SizeQuantityDto(i.Size, i.Quantity)).ToList(),
                g.Sum(i => i.Quantity)
            ))
            .ToList();

        return Result<ProductionOrderQuantityDto>.Success(new ProductionOrderQuantityDto(
            batch.Id, batch.BatchName, batch.TotalQuantity, outfits));
    }
}
