using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>School views distribution status for a production order's campaign orders.</summary>
public record GetDistributionStatusQuery(Guid UserId, Guid BatchId);

public interface IGetDistributionStatusQueryHandler
{
    Task<Result<DistributionStatusResponse>> HandleAsync(GetDistributionStatusQuery query, CancellationToken ct = default);
}

public record DistributionStatusResponse(
    Guid BatchId,
    int TotalOrders,
    int DistributedCount,
    int PendingCount,
    List<DistributionOrderDto> Orders);

public record DistributionOrderDto(
    Guid OrderId,
    string ChildName,
    string ParentName,
    string DeliveryMethod,
    string OrderStatus,
    bool IsDistributed,
    DateTime? DistributedAt,
    string? ShippingCompany,
    string? TrackingCode,
    string? ProofImageUrl);

public class GetDistributionStatusQueryHandler : IGetDistributionStatusQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetDistributionStatusQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DistributionStatusResponse>> HandleAsync(GetDistributionStatusQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.SchoolID == null)
            return Result<DistributionStatusResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Include(b => b.Campaign)
            .FirstOrDefaultAsync(b => b.Id == query.BatchId
                && b.Campaign.SchoolID == user.SchoolID.Value
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<DistributionStatusResponse>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        // Get all campaign orders (excluding cancelled)
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile).ThenInclude(c => c.ParentUser)
            .Where(o => o.CampaignID == batch.CampaignID
                && o.OrderStatus != OrderStatus.Cancelled)
            .ToListAsync(ct);

        // Get distribution records for this batch
        var distributions = await _db.DistributionRecords
            .AsNoTracking()
            .Where(dr => dr.BatchID == batch.Id)
            .ToDictionaryAsync(dr => dr.OrderID, ct);

        var orderDtos = orders.Select(o =>
        {
            distributions.TryGetValue(o.Id, out var dist);
            return new DistributionOrderDto(
                o.Id,
                o.ChildProfile?.FullName ?? "Unknown",
                o.ChildProfile?.ParentUser?.FullName ?? "Unknown",
                o.DeliveryMethod ?? "AtSchool",
                o.OrderStatus.ToString(),
                dist != null,
                dist?.DistributedAt,
                dist?.ShippingCompany,
                dist?.TrackingCode,
                dist?.ProofImageUrl);
        }).OrderBy(o => o.IsDistributed).ThenBy(o => o.ChildName).ToList();

        var distributedCount = orderDtos.Count(o => o.IsDistributed);

        return Result<DistributionStatusResponse>.Success(new DistributionStatusResponse(
            batch.Id,
            orders.Count,
            distributedCount,
            orders.Count - distributedCount,
            orderDtos));
    }
}
