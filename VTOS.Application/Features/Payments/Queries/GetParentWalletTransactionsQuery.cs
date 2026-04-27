using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

public record GetParentWalletTransactionsQuery(Guid UserId, int Page = 1, int PageSize = 20);

public interface IGetParentWalletTransactionsQueryHandler
{
    Task<Result<WalletTransactionsResponse>> HandleAsync(GetParentWalletTransactionsQuery query, CancellationToken ct = default);
}

public class GetParentWalletTransactionsQueryHandler : IGetParentWalletTransactionsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetParentWalletTransactionsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<WalletTransactionsResponse>> HandleAsync(GetParentWalletTransactionsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null || user.Role?.RoleName != "Parent")
            return Result<WalletTransactionsResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var wallet = await _db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == query.UserId && w.OwnerType == WalletOwnerType.Parent && w.IsActive, ct);

        if (wallet == null)
            return Result<WalletTransactionsResponse>.Success(new WalletTransactionsResponse(new(), 0));

        var paymentQuery = _db.PaymentTransactions
            .AsNoTracking()
            .Where(pt => pt.WalletID == wallet.Id)
            .OrderByDescending(pt => pt.TransactionTimestamp);

        var total = await paymentQuery.CountAsync(ct);
        var items = await paymentQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(pt => new WalletTransactionDto(
                pt.Id,
                pt.TransactionType.ToString(),
                pt.Amount,
                pt.TransactionStatus.ToString(),
                pt.Description,
                pt.TransactionTimestamp,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null))
            .ToListAsync(ct);

        return Result<WalletTransactionsResponse>.Success(new WalletTransactionsResponse(items, total));
    }
}
