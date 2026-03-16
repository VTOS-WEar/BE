using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetWalletTransactionsQuery ──────────────────────────────────────
public record GetWalletTransactionsQuery(Guid UserId, int Page = 1, int PageSize = 20);

public record WalletTransactionDto(
    Guid PaymentId,
    string TransactionType,
    decimal Amount,
    string Status,
    string? Description,
    DateTime Timestamp
);

public record WalletTransactionsResponse(List<WalletTransactionDto> Items, int Total);

public interface IGetWalletTransactionsQueryHandler
{
    Task<Result<WalletTransactionsResponse>> HandleAsync(GetWalletTransactionsQuery query, CancellationToken ct = default);
}

public class GetWalletTransactionsQueryHandler : IGetWalletTransactionsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetWalletTransactionsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<WalletTransactionsResponse>> HandleAsync(GetWalletTransactionsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null || user.SchoolID == null)
            return Result<WalletTransactionsResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var wallet = await _db.SchoolWallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.SchoolID == user.SchoolID && w.IsActive, ct);
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
