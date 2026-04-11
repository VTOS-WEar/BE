using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetParentPaymentHistoryQuery ────────────────────────────────────
public record GetParentPaymentHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20, DateTime? StartDate = null, DateTime? EndDate = null, string? Status = null);

public record ParentPaymentDto(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Status,
    string OrderStatus,
    DateTime Timestamp
);

public record StatusCountDto(string Status, int Count);

public record ParentPaymentHistoryResponse(
    List<ParentPaymentDto> Items, 
    int Total,
    List<StatusCountDto> StatusCounts
);

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
            return Result<ParentPaymentHistoryResponse>.Success(
                new ParentPaymentHistoryResponse(new(), 0, new()));

        // Get order IDs for this parent's children
        var orderIds = await _db.Orders.AsNoTracking()
            .Where(o => childIds.Contains(o.ChildProfileID))
            .Select(o => o.Id)
            .ToListAsync(ct);

        var transactions = _db.PaymentTransactions.AsNoTracking()
            .Where(pt => pt.OrderID != null && orderIds.Contains(pt.OrderID.Value) && pt.TransactionType == TransactionType.OrderPayment);

        // Apply Date Filters
        if (query.StartDate.HasValue)
        {
            var start = query.StartDate.Value.Date;
            transactions = transactions.Where(pt => pt.TransactionTimestamp >= start);
        }
        if (query.EndDate.HasValue)
        {
            var end = query.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            transactions = transactions.Where(pt => pt.TransactionTimestamp <= end);
        }

        var q = transactions.Join(_db.Orders.AsNoTracking(), pt => pt.OrderID, o => o.Id, (pt, o) => new { pt, o });

        // Apply Status Filter
        if (!string.IsNullOrEmpty(query.Status))
        {
            if (Enum.TryParse<OrderStatus>(query.Status, true, out var orderStatus))
            {
                q = q.Where(x => x.o.OrderStatus == orderStatus);
            }
        }

        q = q.OrderByDescending(x => x.pt.TransactionTimestamp);

        // Get total count
        var total = await q.CountAsync(ct);

        // Calculate status counts from all orders (not just paginated)
        var allStatusCounts = await q
            .GroupBy(x => x.o.OrderStatus)
            .Select(g => new StatusCountDto(g.Key.ToString(), g.Count()))
            .ToListAsync(ct);

        // Get paginated items
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ParentPaymentDto(
                x.pt.Id,
                x.pt.OrderID!.Value,
                x.pt.Amount,
                x.pt.TransactionStatus.ToString(),
                x.o.OrderStatus.ToString(),
                x.pt.TransactionTimestamp
            ))
            .ToListAsync(ct);

        return Result<ParentPaymentHistoryResponse>.Success(
            new ParentPaymentHistoryResponse(items, total, allStatusCounts));
    }
}
