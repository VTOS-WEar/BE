using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderProductionOrderDetailQuery(Guid UserId, Guid BatchId);

public record ProviderProductionOrderDetailDto(
    Guid BatchId,
    string BatchName,
    string CampaignName,
    Guid CampaignId,
    string SchoolName,
    Guid SchoolId,
    string Status,
    int TotalQuantity,
    DateTime? DeliveryDeadline,
    DateTime? ProcessedAt,
    string? RejectionReason,
    DateTime CreatedDate,
    IReadOnlyList<ProductionBatchItemDto> Items
);

public interface IGetProviderProductionOrderDetailQueryHandler
{
    Task<Result<ProviderProductionOrderDetailDto>> HandleAsync(GetProviderProductionOrderDetailQuery query, CancellationToken ct = default);
}

public class GetProviderProductionOrderDetailQueryHandler : IGetProviderProductionOrderDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderProductionOrderDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ProviderProductionOrderDetailDto>> HandleAsync(GetProviderProductionOrderDetailQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.ProviderID == null)
            return Result<ProviderProductionOrderDetailDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Include(b => b.Campaign).ThenInclude(c => c.School)
            .Include(b => b.Items).ThenInclude(i => i.Outfit)
            .FirstOrDefaultAsync(b => b.Id == query.BatchId
                && b.ProviderID == user.ProviderID.Value
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<ProviderProductionOrderDetailDto>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        var itemDtos = batch.Items
            .Select(i => new ProductionBatchItemDto(
                i.Id, i.OutfitID, i.Outfit.OutfitName, i.Size, i.Quantity, i.UnitPrice
            ))
            .ToList();

        return Result<ProviderProductionOrderDetailDto>.Success(new ProviderProductionOrderDetailDto(
            batch.Id, batch.BatchName, batch.Campaign.CampaignName, batch.CampaignID,
            batch.Campaign.School.SchoolName, batch.Campaign.SchoolID,
            batch.Status.ToString(), batch.TotalQuantity,
            batch.DeliveryDeadline, batch.ProcessedAt, batch.RejectionReason,
            batch.CreatedDate, itemDtos
        ));
    }
}
