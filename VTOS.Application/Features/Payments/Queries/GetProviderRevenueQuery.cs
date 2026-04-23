using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

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
        var providerMgr = await _db.ProviderManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == query.UserId, ct);
        if (providerMgr == null)
            return Result<ProviderRevenueDto>.Failure("Access denied.", "ACCESS_DENIED");

        var providerId = providerMgr.ProviderID;

        var wallet = await _db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == providerId && w.OwnerType == WalletOwnerType.Provider, ct);

        var paidOrderIds = new HashSet<Guid>();
        decimal totalRevenue = 0;

        if (wallet != null)
        {
            var providerPayments = await _db.PaymentTransactions.AsNoTracking()
                .Where(pt => pt.WalletID == wallet.Id
                          && (pt.TransactionType == TransactionType.ProviderPayout
                           || pt.TransactionType == TransactionType.ProviderPayment)
                          && pt.TransactionStatus == PaymentStatus.Completed)
                .ToListAsync(ct);

            totalRevenue = providerPayments.Sum(pt => pt.Amount);
            paidOrderIds = providerPayments
                .Where(pt => pt.OrderID.HasValue)
                .Select(pt => pt.OrderID!.Value)
                .ToHashSet();
        }

        // 4. Pending = direct provider orders that reached a payable fulfillment state
        // but do not yet have a completed provider-payment transaction.
        var pendingOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.ProviderID == providerId
                      && o.SemesterPublicationID != null
                      && (o.OrderStatus == OrderStatus.Paid
                       || o.OrderStatus == OrderStatus.Confirmed
                       || o.OrderStatus == OrderStatus.Processed
                       || o.OrderStatus == OrderStatus.Shipped
                       || o.OrderStatus == OrderStatus.Delivered)
                      && !paidOrderIds.Contains(o.Id))
            .ToListAsync(ct);

        return Result<ProviderRevenueDto>.Success(new ProviderRevenueDto(
            TotalRevenue: totalRevenue,
            TotalPaidOrders: paidOrderIds.Count,
            TotalPendingOrders: pendingOrders.Count,
            PendingAmount: pendingOrders.Sum(o => o.TotalAmount)
        ));
    }
}
