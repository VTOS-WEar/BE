using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

public record GenerateProductionOrderRequest(
    Guid ProviderID,
    string BatchName,
    DateTime DeliveryDeadline
);

public record GenerateProductionOrderCommand(Guid UserId, Guid CampaignId, GenerateProductionOrderRequest Request);

public record GenerateProductionOrderResponseDto(
    Guid BatchId,
    string BatchName,
    string Status,
    int TotalQuantity,
    DateTime DeliveryDeadline,
    DateTime CreatedDate
);

public interface IGenerateProductionOrderCommandHandler
{
    Task<Result<GenerateProductionOrderResponseDto>> HandleAsync(GenerateProductionOrderCommand command, CancellationToken ct = default);
}

public class GenerateProductionOrderCommandHandler : IGenerateProductionOrderCommandHandler
{
    private readonly IApplicationDbContext _db;

    public GenerateProductionOrderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GenerateProductionOrderResponseDto>> HandleAsync(GenerateProductionOrderCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user?.SchoolID == null)
            return Result<GenerateProductionOrderResponseDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var campaign = await _db.Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CampaignId && c.SchoolID == user.SchoolID.Value, ct);
        if (campaign == null)
            return Result<GenerateProductionOrderResponseDto>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        if (campaign.Status != CampaignStatus.Locked)
            return Result<GenerateProductionOrderResponseDto>.Failure(
                "Campaign must be Locked before generating a production order.", "CAMPAIGN_NOT_LOCKED");

        // Verify provider exists
        var providerExists = await _db.Providers.AsNoTracking()
            .AnyAsync(p => p.Id == command.Request.ProviderID && !p.IsDeleted, ct);
        if (!providerExists)
            return Result<GenerateProductionOrderResponseDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        // Aggregate order items for production
        var orderItems = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.ProductVariant)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.CampaignID == command.CampaignId)
            .ToListAsync(ct);

        if (!orderItems.Any())
            return Result<GenerateProductionOrderResponseDto>.Failure("No orders found for this campaign.", "NO_ORDERS");

        var totalQty = orderItems.Sum(oi => oi.Quantity);

        var batch = new ProductionBatch
        {
            Id = Guid.NewGuid(),
            CampaignID = command.CampaignId,
            ProviderID = command.Request.ProviderID,
            BatchName = command.Request.BatchName,
            TotalQuantity = totalQty,
            CreatedDate = DateTime.UtcNow,
            Status = ProductionBatchStatus.Pending,
            DeliveryDeadline = command.Request.DeliveryDeadline.ToUniversalTime()
        };

        _db.ProductionBatches.Add(batch);

        // Create production batch items (per outfit, per size)
        var batchItems = orderItems
            .GroupBy(oi => new { oi.ProductVariant.OutfitID, oi.SizeOrdered })
            .Select(g => new ProductionBatchItem
            {
                Id = Guid.NewGuid(),
                BatchID = batch.Id,
                OutfitID = g.Key.OutfitID,
                Size = g.Key.SizeOrdered,
                Quantity = g.Sum(x => x.Quantity),
                UnitPrice = g.Average(x => x.UnitPrice)
            });

        foreach (var item in batchItems)
            _db.ProductionBatchItems.Add(item);

        await _db.SaveChangesAsync(ct);

        return Result<GenerateProductionOrderResponseDto>.Success(new GenerateProductionOrderResponseDto(
            batch.Id, batch.BatchName, batch.Status.ToString(),
            batch.TotalQuantity, batch.DeliveryDeadline!.Value, batch.CreatedDate
        ));
    }
}
