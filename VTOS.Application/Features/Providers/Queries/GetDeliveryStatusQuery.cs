using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Providers.Queries;

/// <summary>Provider views delivery status for a production order.</summary>
public record GetDeliveryStatusQuery(Guid UserId, Guid BatchId);

public interface IGetDeliveryStatusQueryHandler
{
    Task<Result<DeliveryStatusResponse>> HandleAsync(GetDeliveryStatusQuery query, CancellationToken ct = default);
}

public record DeliveryStatusResponse(
    Guid BatchId,
    string BatchName,
    int TotalQuantity,
    int TotalDelivered,
    bool IsFullyDelivered,
    DateTime? DeliveryConfirmedAt,
    List<DeliveryRecordDto> Deliveries);

public record DeliveryRecordDto(
    Guid Id,
    int Quantity,
    string? Note,
    DateTime DeliveredAt,
    bool IsConfirmed,
    DateTime? ConfirmedAt,
    int? AcceptedQuantity,
    int? DefectiveQuantity,
    string? DefectNote);

public class GetDeliveryStatusQueryHandler : IGetDeliveryStatusQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetDeliveryStatusQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DeliveryStatusResponse>> HandleAsync(GetDeliveryStatusQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.ProviderID == null)
            return Result<DeliveryStatusResponse>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Include(b => b.DeliveryRecords)
            .FirstOrDefaultAsync(b => b.Id == query.BatchId
                && b.ProviderID == user.ProviderID.Value
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<DeliveryStatusResponse>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        var deliveries = batch.DeliveryRecords
            .OrderByDescending(dr => dr.DeliveredAt)
            .Select(dr => new DeliveryRecordDto(
                dr.Id,
                dr.Quantity,
                dr.Note,
                dr.DeliveredAt,
                dr.IsConfirmed,
                dr.ConfirmedAt,
                dr.AcceptedQuantity,
                dr.DefectiveQuantity,
                dr.DefectNote))
            .ToList();

        return Result<DeliveryStatusResponse>.Success(new DeliveryStatusResponse(
            batch.Id,
            batch.BatchName,
            batch.TotalQuantity,
            batch.DeliveredQuantity,
            batch.DeliveredQuantity >= batch.TotalQuantity,
            batch.DeliveryConfirmedAt,
            deliveries));
    }
}
