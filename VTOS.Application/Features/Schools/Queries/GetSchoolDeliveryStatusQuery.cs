using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>School views delivery status for a production order.</summary>
public record GetSchoolDeliveryStatusQuery(Guid UserId, Guid BatchId);

public interface IGetSchoolDeliveryStatusQueryHandler
{
    Task<Result<SchoolDeliveryStatusResponse>> HandleAsync(GetSchoolDeliveryStatusQuery query, CancellationToken ct = default);
}

public record SchoolDeliveryStatusResponse(
    Guid BatchId,
    string BatchName,
    int TotalQuantity,
    int TotalDelivered,
    bool IsFullyDelivered,
    DateTime? DeliveryConfirmedAt,
    List<SchoolDeliveryRecordDto> Deliveries);

public record SchoolDeliveryRecordDto(
    Guid Id,
    int Quantity,
    string? Note,
    DateTime DeliveredAt,
    bool IsConfirmed,
    DateTime? ConfirmedAt,
    int? AcceptedQuantity,
    int? DefectiveQuantity,
    string? DefectNote);

public class GetSchoolDeliveryStatusQueryHandler : IGetSchoolDeliveryStatusQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolDeliveryStatusQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SchoolDeliveryStatusResponse>> HandleAsync(GetSchoolDeliveryStatusQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.SchoolID == null)
            return Result<SchoolDeliveryStatusResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Include(b => b.DeliveryRecords)
            .Include(b => b.Campaign)
            .FirstOrDefaultAsync(b => b.Id == query.BatchId
                && b.Campaign.SchoolID == user.SchoolID.Value
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<SchoolDeliveryStatusResponse>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        var deliveries = batch.DeliveryRecords
            .OrderByDescending(dr => dr.DeliveredAt)
            .Select(dr => new SchoolDeliveryRecordDto(
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

        return Result<SchoolDeliveryStatusResponse>.Success(new SchoolDeliveryStatusResponse(
            batch.Id,
            batch.BatchName,
            batch.TotalQuantity,
            batch.DeliveredQuantity,
            batch.DeliveredQuantity >= batch.TotalQuantity,
            batch.DeliveryConfirmedAt,
            deliveries));
    }
}
