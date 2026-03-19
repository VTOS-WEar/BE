using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetProviderPaymentHistoryQuery ──────────────────────────────────
public record GetProviderPaymentHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20);

public record ProviderPaymentDto(
    Guid PaymentId,
    Guid? OrderId,
    decimal Amount,
    string Status,
    string? Description,
    DateTime Timestamp
);

public record ProviderPaymentHistoryResponse(List<ProviderPaymentDto> Items, int Total);

public interface IGetProviderPaymentHistoryQueryHandler
{
    Task<Result<ProviderPaymentHistoryResponse>> HandleAsync(GetProviderPaymentHistoryQuery query, CancellationToken ct = default);
}

public class GetProviderPaymentHistoryQueryHandler : IGetProviderPaymentHistoryQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderPaymentHistoryQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProviderPaymentHistoryResponse>> HandleAsync(GetProviderPaymentHistoryQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null)
            return Result<ProviderPaymentHistoryResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        var providerId = providerMgr?.ProviderID;

        // Get campaigns for this provider
        var campaignIds = await _db.CampaignOutfits.AsNoTracking()
            .Where(co => co.ProviderID == providerId)
            .Select(co => co.CampaignID)
            .Distinct()
            .ToListAsync(ct);

        // Get orders from those campaigns
        var orderIds = await _db.Orders.AsNoTracking()
            .Where(o => o.CampaignID != null && campaignIds.Contains(o.CampaignID.Value))
            .Select(o => o.Id)
            .ToListAsync(ct);

        // Get ProviderPayment transactions for those orders
        var q = _db.PaymentTransactions.AsNoTracking()
            .Where(pt => pt.OrderID != null && orderIds.Contains(pt.OrderID.Value)
                && pt.TransactionType == TransactionType.ProviderPayment)
            .OrderByDescending(pt => pt.TransactionTimestamp);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(pt => new ProviderPaymentDto(
                pt.Id,
                pt.OrderID,
                pt.Amount,
                pt.TransactionStatus.ToString(),
                pt.Description,
                pt.TransactionTimestamp
            ))
            .ToListAsync(ct);

        return Result<ProviderPaymentHistoryResponse>.Success(new ProviderPaymentHistoryResponse(items, total));
    }
}
