using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetParentPaymentHistoryQuery ────────────────────────────────────
public record GetParentPaymentHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20);

public record ParentPaymentDto(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Status,
    DateTime Timestamp
);

public record ParentPaymentHistoryResponse(List<ParentPaymentDto> Items, int Total);

public interface IGetParentPaymentHistoryQueryHandler
{
    Task<Result<ParentPaymentHistoryResponse>> HandleAsync(GetParentPaymentHistoryQuery query, CancellationToken ct = default);
}

public class GetParentPaymentHistoryQueryHandler : IGetParentPaymentHistoryQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetParentPaymentHistoryQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ParentPaymentHistoryResponse>> HandleAsync(GetParentPaymentHistoryQuery query, CancellationToken ct = default)
    {
        // Get all child profiles belonging to this parent
        var childIds = await _db.ChildProfiles.AsNoTracking()
            .Where(c => c.ParentUserID == query.UserId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (!childIds.Any())
            return Result<ParentPaymentHistoryResponse>.Success(new ParentPaymentHistoryResponse(new(), 0));

        // Get order IDs for this parent's children
        var orderIds = await _db.Orders.AsNoTracking()
            .Where(o => childIds.Contains(o.ChildProfileID))
            .Select(o => o.Id)
            .ToListAsync(ct);

        var q = _db.PaymentTransactions.AsNoTracking()
            .Where(pt => pt.OrderID != null && orderIds.Contains(pt.OrderID.Value) && pt.TransactionType == TransactionType.OrderPayment)
            .OrderByDescending(pt => pt.TransactionTimestamp);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(pt => new ParentPaymentDto(
                pt.Id,
                pt.OrderID!.Value,
                pt.Amount,
                pt.TransactionStatus.ToString(),
                pt.TransactionTimestamp
            ))
            .ToListAsync(ct);

        return Result<ParentPaymentHistoryResponse>.Success(new ParentPaymentHistoryResponse(items, total));
    }
}
