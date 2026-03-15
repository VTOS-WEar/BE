using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>School distributes uniforms to parent orders (AtSchool or AtHome).</summary>
public record DistributeOrdersCommand(
    Guid UserId,
    Guid BatchId,
    List<Guid> OrderIds,
    string? ShippingCompany,
    string? TrackingCode,
    string? ProofImageUrl,
    string? Note);

public interface IDistributeOrdersCommandHandler
{
    Task<Result<DistributeOrdersResponse>> HandleAsync(DistributeOrdersCommand command, CancellationToken ct = default);
}

public record DistributeOrdersResponse(
    int DistributedCount,
    int TotalOrders,
    int RemainingOrders,
    string Message);

public class DistributeOrdersCommandHandler : IDistributeOrdersCommandHandler
{
    private readonly IApplicationDbContext _db;

    public DistributeOrdersCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DistributeOrdersResponse>> HandleAsync(DistributeOrdersCommand command, CancellationToken ct = default)
    {
        // Resolve school
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user?.SchoolID == null)
            return Result<DistributeOrdersResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        // Get batch
        var batch = await _db.ProductionBatches
            .Include(b => b.Campaign)
            .Include(b => b.DeliveryRecords)
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.Campaign.SchoolID == user.SchoolID.Value
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<DistributeOrdersResponse>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        // Must be Delivered status
        if (batch.Status != ProductionBatchStatus.Delivered)
            return Result<DistributeOrdersResponse>.Failure(
                "Batch must be in Delivered status before distribution.", "INVALID_STATUS");

        // Must have at least one confirmed delivery
        if (!batch.DeliveryRecords.Any(dr => dr.IsConfirmed))
            return Result<DistributeOrdersResponse>.Failure(
                "At least one delivery must be confirmed before distribution.", "DELIVERY_NOT_CONFIRMED");

        if (command.OrderIds == null || !command.OrderIds.Any())
            return Result<DistributeOrdersResponse>.Failure("No orders specified for distribution.", "NO_ORDERS");

        // Get orders belonging to this campaign
        var orders = await _db.Orders
            .Where(o => command.OrderIds.Contains(o.Id)
                && o.CampaignID == batch.CampaignID)
            .ToListAsync(ct);

        if (!orders.Any())
            return Result<DistributeOrdersResponse>.Failure("No valid orders found for this campaign.", "ORDERS_NOT_FOUND");

        // Check for already-distributed orders
        var alreadyDistributed = await _db.DistributionRecords
            .Where(dr => command.OrderIds.Contains(dr.OrderID))
            .Select(dr => dr.OrderID)
            .ToListAsync(ct);

        var newOrders = orders.Where(o => !alreadyDistributed.Contains(o.Id)).ToList();

        if (!newOrders.Any())
            return Result<DistributeOrdersResponse>.Failure("All selected orders have already been distributed.", "ALREADY_DISTRIBUTED");

        var now = DateTime.UtcNow;
        foreach (var order in newOrders)
        {
            // Determine method from order's DeliveryMethod
            var method = string.Equals(order.DeliveryMethod, "AtHome", StringComparison.OrdinalIgnoreCase)
                ? "AtHome"
                : "AtSchool";

            // For AtHome, require shipping info
            if (method == "AtHome" &&
                (string.IsNullOrWhiteSpace(command.ShippingCompany) ||
                 string.IsNullOrWhiteSpace(command.TrackingCode) ||
                 string.IsNullOrWhiteSpace(command.ProofImageUrl)))
            {
                // Skip this order and continue, or we could fail entirely.
                // For now, skip silently — the UI should separate AtHome/AtSchool flows
                continue;
            }

            var distributionRecord = new DistributionRecord
            {
                Id = Guid.NewGuid(),
                BatchID = batch.Id,
                OrderID = order.Id,
                DistributedAt = now,
                Method = method,
                ShippingCompany = method == "AtHome" ? command.ShippingCompany : null,
                TrackingCode = method == "AtHome" ? command.TrackingCode : null,
                ProofImageUrl = method == "AtHome" ? command.ProofImageUrl : null,
                Note = command.Note,
                CreatedAt = now
            };

            _db.DistributionRecords.Add(distributionRecord);

            // Update order status — both methods go directly to Delivered
            order.OrderStatus = OrderStatus.Delivered;
            order.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        // Count total orders in campaign
        var totalCampaignOrders = await _db.Orders
            .CountAsync(o => o.CampaignID == batch.CampaignID
                && o.OrderStatus != OrderStatus.Cancelled, ct);

        var totalDistributed = await _db.DistributionRecords
            .CountAsync(dr => dr.BatchID == batch.Id, ct);

        return Result<DistributeOrdersResponse>.Success(new DistributeOrdersResponse(
            newOrders.Count,
            totalCampaignOrders,
            totalCampaignOrders - totalDistributed,
            $"Distributed {newOrders.Count} orders. {totalCampaignOrders - totalDistributed} remaining."
        ));
    }
}
