using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class MonitorPaymentTransactionsQueryHandler : IMonitorPaymentTransactionsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public MonitorPaymentTransactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<PaymentTransactionDto>> HandleAsync(
        MonitorPaymentTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var paymentsQuery = _context.PaymentTransactions
            .Include(p => p.Order)
            .AsQueryable();

        // Apply date filters
        if (query.DateFrom.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt <= query.DateTo.Value);

        // Apply status filter - parse enum string first
        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<PaymentStatus>(query.Status, out var statusEnum))
            paymentsQuery = paymentsQuery.Where(p => p.TransactionStatus == statusEnum);

        // Apply payment gateway filter - parse enum string first
        if (!string.IsNullOrEmpty(query.PaymentGateway) && Enum.TryParse<PaymentGatewayType>(query.PaymentGateway, out var gatewayEnum))
            paymentsQuery = paymentsQuery.Where(p => p.GatewayType == gatewayEnum);

        var totalCount = await paymentsQuery.CountAsync(cancellationToken);

        var payments = await paymentsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PaymentTransactionDto(
                p.Id,
                p.OrderID,
                p.GatewayType.ToString(),
                p.TransactionStatus.ToString(),
                p.Amount,
                p.TransactionTimestamp,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<PaymentTransactionDto>(payments, totalCount, query.Page, query.PageSize);
    }
}
