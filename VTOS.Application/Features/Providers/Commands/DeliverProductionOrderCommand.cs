using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

/// <summary>Provider delivers a partial shipment (Completed → track deliveries → auto Delivered at 100%).</summary>
public record DeliverProductionOrderCommand(Guid UserId, Guid BatchId, int Quantity, string? Note);

public interface IDeliverProductionOrderCommandHandler
{
    Task<Result<DeliverProductionOrderResponse>> HandleAsync(DeliverProductionOrderCommand command, CancellationToken ct = default);
}

public record DeliverProductionOrderResponse(
    Guid DeliveryRecordId,
    int DeliveredThisTime,
    int TotalDelivered,
    int TotalRequired,
    bool IsFullyDelivered,
    string Message);

public class DeliverProductionOrderCommandHandler : IDeliverProductionOrderCommandHandler
{
    private readonly IApplicationDbContext _db;

    public DeliverProductionOrderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DeliverProductionOrderResponse>> HandleAsync(DeliverProductionOrderCommand command, CancellationToken ct = default)
    {
        // Resolve provider
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<DeliverProductionOrderResponse>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        // Get batch
        var batch = await _db.ProductionBatches
            .Include(b => b.DeliveryRecords)
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.ProviderID == providerMgr.ProviderID
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<DeliverProductionOrderResponse>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        // Must be Completed or already partially delivered (still Completed)
        if (batch.Status != ProductionBatchStatus.Completed)
            return Result<DeliverProductionOrderResponse>.Failure(
                $"Only Completed batches can be delivered. Current: {batch.Status}.", "INVALID_STATUS");

        // Validate quantity
        if (command.Quantity <= 0)
            return Result<DeliverProductionOrderResponse>.Failure("Quantity must be greater than 0.", "INVALID_QUANTITY");

        var alreadyDelivered = batch.DeliveryRecords.Sum(dr => dr.Quantity);
        var remaining = batch.TotalQuantity - alreadyDelivered;

        if (command.Quantity > remaining)
            return Result<DeliverProductionOrderResponse>.Failure(
                $"Cannot deliver {command.Quantity}. Only {remaining} remaining out of {batch.TotalQuantity}.", "EXCEEDS_QUANTITY");

        // Create delivery record
        var deliveryRecord = new DeliveryRecord
        {
            Id = Guid.NewGuid(),
            BatchID = batch.Id,
            Quantity = command.Quantity,
            Note = command.Note,
            DeliveredAt = DateTime.UtcNow,
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.DeliveryRecords.Add(deliveryRecord);

        // Update running total
        var newTotal = alreadyDelivered + command.Quantity;
        batch.DeliveredQuantity = newTotal;

        // Auto-change to Delivered when 100%
        bool isFullyDelivered = newTotal >= batch.TotalQuantity;
        if (isFullyDelivered)
        {
            batch.Status = ProductionBatchStatus.Delivered;
            batch.DeliveryNote = command.Note;
        }

        await _db.SaveChangesAsync(ct);

        return Result<DeliverProductionOrderResponse>.Success(new DeliverProductionOrderResponse(
            deliveryRecord.Id,
            command.Quantity,
            newTotal,
            batch.TotalQuantity,
            isFullyDelivered,
            isFullyDelivered
                ? "All uniforms delivered! Batch status changed to Delivered."
                : $"Partial delivery recorded. {batch.TotalQuantity - newTotal} remaining."
        ));
    }
}
