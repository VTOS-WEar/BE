using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetProviderWalletTransactionsQuery ──────────────────────────────
public record GetProviderWalletTransactionsQuery(Guid UserId, int Page = 1, int PageSize = 20);

public interface IGetProviderWalletTransactionsQueryHandler
{
    Task<Result<WalletTransactionsResponse>> HandleAsync(GetProviderWalletTransactionsQuery query, CancellationToken ct = default);
}

public class GetProviderWalletTransactionsQueryHandler : IGetProviderWalletTransactionsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderWalletTransactionsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<WalletTransactionsResponse>> HandleAsync(GetProviderWalletTransactionsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null)
            return Result<WalletTransactionsResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr == null)
            return Result<WalletTransactionsResponse>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var wallet = await _db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == providerMgr.ProviderID && w.OwnerType == Domain.Enums.WalletOwnerType.Provider && w.IsActive, ct);
        if (wallet == null)
            return Result<WalletTransactionsResponse>.Success(new WalletTransactionsResponse(new(), 0));

        var q = _db.PaymentTransactions.AsNoTracking()
            .Where(pt => pt.WalletID == wallet.Id)
            .OrderByDescending(pt => pt.TransactionTimestamp);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(pt => new WalletTransactionDto(
                pt.Id,
                pt.TransactionType.ToString(),
                pt.Amount,
                pt.TransactionStatus.ToString(),
                pt.Description,
                pt.TransactionTimestamp
            ))
            .ToListAsync(ct);

        return Result<WalletTransactionsResponse>.Success(new WalletTransactionsResponse(items, total));
    }
}
