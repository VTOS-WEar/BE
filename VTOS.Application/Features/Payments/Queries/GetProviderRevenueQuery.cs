using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetProviderRevenueQuery ─────────────────────────────────────────
public record GetProviderRevenueQuery(Guid UserId);

public record ProviderRevenueDto(
    decimal TotalRevenue,
    int TotalPaidOrders,
    int TotalPendingOrders,
    decimal PendingAmount
);

public interface IGetProviderRevenueQueryHandler
{
    Task<Result<ProviderRevenueDto>> HandleAsync(GetProviderRevenueQuery query, CancellationToken ct = default);
}

public class GetProviderRevenueQueryHandler : IGetProviderRevenueQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderRevenueQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProviderRevenueDto>> HandleAsync(GetProviderRevenueQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null)
            return Result<ProviderRevenueDto>.Failure("Access denied.", "ACCESS_DENIED");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        var providerId = providerMgr?.ProviderID;

        // Get all campaigns assigned to this provider
        var campaignIds = await _db.CampaignOutfits.AsNoTracking()
            .Where(co => co.ProviderID == providerId)
            .Select(co => co.CampaignID)
            .Distinct()
            .ToListAsync(ct);

        // Get all orders from those campaigns
        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.CampaignID != null && campaignIds.Contains(o.CampaignID.Value))
            .ToListAsync(ct);

        var paidOrders = orders.Where(o => o.IsProviderPaid).ToList();
        var pendingOrders = orders.Where(o =>
            (o.OrderStatus == OrderStatus.Paid || o.OrderStatus == OrderStatus.Confirmed ||
             o.OrderStatus == OrderStatus.Processed || o.OrderStatus == OrderStatus.Shipped ||
             o.OrderStatus == OrderStatus.Delivered) && !o.IsProviderPaid).ToList();

        return Result<ProviderRevenueDto>.Success(new ProviderRevenueDto(
            TotalRevenue: paidOrders.Sum(o => o.TotalAmount),
            TotalPaidOrders: paidOrders.Count,
            TotalPendingOrders: pendingOrders.Count,
            PendingAmount: pendingOrders.Sum(o => o.TotalAmount)
        ));
    }
}
