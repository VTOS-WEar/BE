using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetProductionOrderDetailQuery(Guid UserId, Guid BatchId);

public record ProductionBatchItemDto(
    Guid ItemId,
    Guid OutfitId,
    string OutfitName,
    string Size,
    int Quantity,
    decimal UnitPrice
);

public record ProductionOrderDetailDto(
    Guid BatchId,
    string BatchName,
    string CampaignName,
    Guid CampaignId,
    string ProviderName,
    Guid ProviderId,
    string Status,
    int TotalQuantity,
    DateTime? DeliveryDeadline,
    DateTime? ProcessedAt,
    string? RejectionReason,
    DateTime CreatedDate,
    IReadOnlyList<ProductionBatchItemDto> Items
);

public interface IGetProductionOrderDetailQueryHandler
{
    Task<Result<ProductionOrderDetailDto>> HandleAsync(GetProductionOrderDetailQuery query, CancellationToken ct = default);
}

public class GetProductionOrderDetailQueryHandler : IGetProductionOrderDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProductionOrderDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ProductionOrderDetailDto>> HandleAsync(GetProductionOrderDetailQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<ProductionOrderDetailDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Include(b => b.Campaign)
            .Include(b => b.Provider)
            .Include(b => b.Items).ThenInclude(i => i.Outfit)
            .FirstOrDefaultAsync(b => b.Id == query.BatchId
                && b.Campaign.SchoolID == schoolMgr.SchoolID
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<ProductionOrderDetailDto>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        var itemDtos = batch.Items
            .Select(i => new ProductionBatchItemDto(
                i.Id, i.OutfitID, i.Outfit.OutfitName, i.Size, i.Quantity, i.UnitPrice
            ))
            .ToList();

        return Result<ProductionOrderDetailDto>.Success(new ProductionOrderDetailDto(
            batch.Id, batch.BatchName, batch.Campaign.CampaignName, batch.CampaignID,
            batch.Provider.ProviderName, batch.ProviderID,
            batch.Status.ToString(), batch.TotalQuantity,
            batch.DeliveryDeadline, batch.ProcessedAt, batch.RejectionReason,
            batch.CreatedDate, itemDtos
        ));
    }
}
