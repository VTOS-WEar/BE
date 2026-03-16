using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>School verifies delivered quantity vs expected per outfit/size.</summary>
public record GetVerifyQuantityQuery(Guid UserId, Guid BatchId);

public interface IGetVerifyQuantityQueryHandler
{
    Task<Result<VerifyQuantityResponse>> HandleAsync(GetVerifyQuantityQuery query, CancellationToken ct = default);
}

public record VerifyQuantityResponse(
    Guid BatchId,
    int TotalExpected,
    int TotalDelivered,
    int TotalAccepted,
    int TotalDefective,
    List<VerifyQuantityItemDto> Items);

public record VerifyQuantityItemDto(
    Guid OutfitId,
    string OutfitName,
    string Size,
    int Expected,
    int Delivered,
    int Accepted,
    int Defective);

public class GetVerifyQuantityQueryHandler : IGetVerifyQuantityQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetVerifyQuantityQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<VerifyQuantityResponse>> HandleAsync(GetVerifyQuantityQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.SchoolID == null)
            return Result<VerifyQuantityResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Include(b => b.Campaign)
            .Include(b => b.Items).ThenInclude(i => i.Outfit)
            .Include(b => b.DeliveryRecords)
            .FirstOrDefaultAsync(b => b.Id == query.BatchId
                && b.Campaign.SchoolID == user.SchoolID.Value
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<VerifyQuantityResponse>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        // Map expected per outfit/size from BatchItems
        var items = batch.Items.Select(item => new VerifyQuantityItemDto(
            item.OutfitID,
            item.Outfit?.OutfitName ?? "Unknown",
            item.Size,
            item.Quantity,
            0, // Delivered per item — not tracked at item level; only total
            0,
            0
        )).ToList();

        // Aggregate from delivery records
        var totalDelivered = batch.DeliveryRecords.Sum(dr => dr.Quantity);
        var confirmedRecords = batch.DeliveryRecords.Where(dr => dr.IsConfirmed);
        var totalAccepted = confirmedRecords.Sum(dr => dr.AcceptedQuantity ?? 0);
        var totalDefective = confirmedRecords.Sum(dr => dr.DefectiveQuantity ?? 0);

        return Result<VerifyQuantityResponse>.Success(new VerifyQuantityResponse(
            batch.Id,
            batch.TotalQuantity,
            totalDelivered,
            totalAccepted,
            totalDefective,
            items));
    }
}
