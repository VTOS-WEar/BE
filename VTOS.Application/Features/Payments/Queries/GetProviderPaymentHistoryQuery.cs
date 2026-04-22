using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

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
        var providerMgr = await _db.ProviderManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == query.UserId, ct);
        if (providerMgr == null)
            return Result<ProviderPaymentHistoryResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var wallet = await _db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == providerMgr.ProviderID && w.OwnerType == WalletOwnerType.Provider, ct);

        if (wallet == null)
            return Result<ProviderPaymentHistoryResponse>.Success(new ProviderPaymentHistoryResponse(new List<ProviderPaymentDto>(), 0));

        var q = _db.PaymentTransactions.AsNoTracking()
            .Where(pt => pt.WalletID == wallet.Id &&
                         (pt.TransactionType == TransactionType.ProviderPayment ||
                          pt.TransactionType == TransactionType.ProviderPayout))
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
