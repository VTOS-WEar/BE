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
        // 1. Find the provider linked to this user
        var providerMgr = await _db.ProviderManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == query.UserId, ct);
        if (providerMgr == null)
            return Result<ProviderRevenueDto>.Failure("Access denied.", "ACCESS_DENIED");

        var providerId = providerMgr.ProviderID;

        // 2. Find provider's wallet
        var wallet = await _db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == providerId
                                   && w.OwnerType == WalletOwnerType.Provider, ct);

        // 3. Revenue from PaymentTransactions (single source of truth, matches Wallet)
        decimal totalRevenue = 0;
        int totalPaidOrders = 0;
        var paidOrderIds = new HashSet<Guid>();

        if (wallet != null)
        {
            // Get all completed ProviderPayment transactions for this wallet
            var providerPayments = await _db.PaymentTransactions.AsNoTracking()
                .Where(pt => pt.WalletID == wallet.Id
                          && pt.TransactionType == TransactionType.ProviderPayment
                          && pt.TransactionStatus == PaymentStatus.Completed)
                .ToListAsync(ct);

            totalRevenue = providerPayments.Sum(pt => pt.Amount);
            paidOrderIds = providerPayments
                .Where(pt => pt.OrderID.HasValue)
                .Select(pt => pt.OrderID!.Value)
                .ToHashSet();
            totalPaidOrders = paidOrderIds.Count;
        }

        // 4. Pending = orders assigned to this provider that are active but NOT yet paid
        var campaignIds = await _db.CampaignOutfits.AsNoTracking()
            .Where(co => co.ProviderID == providerId)
            .Select(co => co.CampaignID)
            .Distinct()
            .ToListAsync(ct);

        var pendingOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.CampaignID != null
                      && campaignIds.Contains(o.CampaignID.Value)
                      && (o.OrderStatus == OrderStatus.Paid
                       || o.OrderStatus == OrderStatus.Confirmed
                       || o.OrderStatus == OrderStatus.Processed
                       || o.OrderStatus == OrderStatus.Shipped
                       || o.OrderStatus == OrderStatus.Delivered)
                      && !paidOrderIds.Contains(o.Id))
            .ToListAsync(ct);

        return Result<ProviderRevenueDto>.Success(new ProviderRevenueDto(
            TotalRevenue: totalRevenue,
            TotalPaidOrders: totalPaidOrders,
            TotalPendingOrders: pendingOrders.Count,
            PendingAmount: pendingOrders.Sum(o => o.TotalAmount)
        ));
    }
}

