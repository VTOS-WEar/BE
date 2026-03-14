using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class GetPaymentCompletionRateQueryHandler : IGetPaymentCompletionRateQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetPaymentCompletionRateQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentCompletionRateDto> HandleAsync(
        GetPaymentCompletionRateQuery query,
        CancellationToken cancellationToken)
    {
        var paymentsQuery = _context.PaymentTransactions.AsQueryable();

        if (query.DateFrom.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt <= query.DateTo.Value);

        var payments = await paymentsQuery.ToListAsync(cancellationToken);

        var totalAttempts = payments.Count;
        var completedPayments = payments.Count(p => p.TransactionStatus == PaymentStatus.Completed);
        var failedPayments = payments.Count(p => p.TransactionStatus == PaymentStatus.Failed);

        var completionRate = totalAttempts > 0 ? (completedPayments * 100m / totalAttempts) : 0;

        // Payment breakdown by status
        var paymentsByStatus = new List<PaymentStatusBreakdownDto>();
        
        if (totalAttempts > 0)
        {
            paymentsByStatus.Add(new PaymentStatusBreakdownDto(
                "Completed",
                completedPayments,
                (completedPayments * 100m / totalAttempts)
            ));
            
            paymentsByStatus.Add(new PaymentStatusBreakdownDto(
                "Failed",
                failedPayments,
                (failedPayments * 100m / totalAttempts)
            ));

            var pendingCount = totalAttempts - completedPayments - failedPayments;
            if (pendingCount > 0)
            {
                paymentsByStatus.Add(new PaymentStatusBreakdownDto(
                    "Pending",
                    pendingCount,
                    (pendingCount * 100m / totalAttempts)
                ));
            }
        }

        return new PaymentCompletionRateDto(
            totalAttempts,
            completedPayments,
            failedPayments,
            completionRate,
            paymentsByStatus
        );
    }
}
