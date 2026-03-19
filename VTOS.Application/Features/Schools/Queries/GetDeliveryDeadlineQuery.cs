using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>UC 3.9.16 — View delivery deadline for a production order.</summary>
public record GetDeliveryDeadlineQuery(Guid UserId, Guid BatchId);

public record DeliveryDeadlineDto(
    Guid BatchId,
    string BatchName,
    string Status,
    DateTime? DeliveryDeadline,
    int DaysRemaining
);

public interface IGetDeliveryDeadlineQueryHandler
{
    Task<Result<DeliveryDeadlineDto>> HandleAsync(GetDeliveryDeadlineQuery query, CancellationToken ct = default);
}

public class GetDeliveryDeadlineQueryHandler : IGetDeliveryDeadlineQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetDeliveryDeadlineQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DeliveryDeadlineDto>> HandleAsync(GetDeliveryDeadlineQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<DeliveryDeadlineDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.Campaign.SchoolID == schoolMgr.SchoolID && !b.IsDeleted, ct);

        if (batch == null)
            return Result<DeliveryDeadlineDto>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        var daysRemaining = batch.DeliveryDeadline.HasValue
            ? (int)(batch.DeliveryDeadline.Value - DateTime.UtcNow).TotalDays
            : -1;

        return Result<DeliveryDeadlineDto>.Success(new DeliveryDeadlineDto(
            batch.Id, batch.BatchName, batch.Status.ToString(),
            batch.DeliveryDeadline, daysRemaining
        ));
    }
}
