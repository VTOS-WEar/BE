using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class GetSemesterMonitorReportQueryHandler : IGetSemesterMonitorReportQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetSemesterMonitorReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SemesterMonitorReportDto>> HandleAsync(
        GetSemesterMonitorReportQuery query,
        CancellationToken ct = default)
    {
        var publication = await _context.SemesterPublications
            .AsNoTracking()
            .Include(x => x.School)
            .FirstOrDefaultAsync(x => x.Id == query.SemesterPublicationId, ct);

        if (publication == null)
        {
            return Result<SemesterMonitorReportDto>.Failure(
                "Semester publication not found.",
                "SEMESTER_PUBLICATION_NOT_FOUND");
        }

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(x => x.SemesterPublicationID == query.SemesterPublicationId)
            .Include(x => x.ChildProfile)
                .ThenInclude(x => x.School)
            .Include(x => x.Provider)
            .Include(x => x.PaymentTransactions)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var payments = orders
            .SelectMany(x => x.PaymentTransactions)
            .ToList();
        var orderPayments = payments
            .Where(x => x.TransactionType == TransactionType.OrderPayment)
            .ToList();
        var completedOrderPayments = orderPayments
            .Where(x => x.TransactionStatus == PaymentStatus.Completed)
            .ToList();

        var totalOrders = orders.Count;
        var completedOrders = orders.Count(x => x.OrderStatus == OrderStatus.Delivered);
        var refundedOrders = orders.Count(x => x.OrderStatus == OrderStatus.Refunded);
        var cancelledOrders = orders.Count(x => x.OrderStatus == OrderStatus.Cancelled);
        var openOrders = totalOrders - completedOrders - refundedOrders - cancelledOrders;

        var completedPayments = completedOrderPayments.Count;
        var totalPaymentAttempts = orderPayments.Count;

        var summary = new SemesterMonitorSummaryDto(
            totalOrders,
            completedOrders,
            refundedOrders,
            cancelledOrders,
            openOrders,
            CalculateRate(completedOrders, totalOrders),
            CalculateRate(refundedOrders, totalOrders),
            CalculateRate(cancelledOrders, totalOrders),
            completedOrderPayments.Sum(x => x.Amount),
            payments
                .Where(x => x.TransactionType == TransactionType.Refund
                    && x.TransactionStatus == PaymentStatus.Completed)
                .Sum(x => x.Amount),
            totalPaymentAttempts,
            completedPayments,
            CalculateRate(completedPayments, totalPaymentAttempts)
        );

        var report = new SemesterMonitorReportDto(
            new SemesterMonitorPublicationDto(
                publication.Id,
                publication.Semester,
                publication.AcademicYear,
                publication.SchoolID,
                publication.School.SchoolName,
                publication.Status.ToString(),
                publication.StartDate,
                publication.EndDate
            ),
            summary,
            BuildOrderStatusBreakdown(orders),
            BuildPaymentStatusBreakdown(orderPayments),
            orders.Select(ToOrderDetail).ToList()
        );

        return Result<SemesterMonitorReportDto>.Success(report);
    }

    private static List<SemesterMonitorStatusMetricDto> BuildOrderStatusBreakdown(List<Order> orders)
    {
        var total = orders.Count;

        return Enum.GetValues<OrderStatus>()
            .Select(status =>
            {
                var matchingOrders = orders.Where(x => x.OrderStatus == status).ToList();
                return new SemesterMonitorStatusMetricDto(
                    status.ToString(),
                    matchingOrders.Count,
                    CalculateRate(matchingOrders.Count, total),
                    matchingOrders.Sum(x => x.TotalAmount)
                );
            })
            .Where(x => x.Count > 0)
            .ToList();
    }

    private static List<SemesterMonitorStatusMetricDto> BuildPaymentStatusBreakdown(List<PaymentTransaction> payments)
    {
        var total = payments.Count;

        return Enum.GetValues<PaymentStatus>()
            .Select(status =>
            {
                var matchingPayments = payments.Where(x => x.TransactionStatus == status).ToList();
                return new SemesterMonitorStatusMetricDto(
                    status.ToString(),
                    matchingPayments.Count,
                    CalculateRate(matchingPayments.Count, total),
                    matchingPayments.Sum(x => x.Amount)
                );
            })
            .Where(x => x.Count > 0)
            .ToList();
    }

    private static SemesterMonitorOrderDetailDto ToOrderDetail(Order order)
    {
        var latestPayment = order.PaymentTransactions
            .OrderByDescending(x => x.TransactionTimestamp)
            .FirstOrDefault();
        var paidAt = order.PaymentTransactions
            .Where(x => x.TransactionStatus == PaymentStatus.Completed)
            .OrderByDescending(x => x.TransactionTimestamp)
            .Select(x => (DateTime?)x.TransactionTimestamp)
            .FirstOrDefault();

        return new SemesterMonitorOrderDetailDto(
            order.Id,
            order.Id.ToString("N")[..8].ToUpperInvariant(),
            order.ChildProfile.FullName,
            order.ChildProfile.School.SchoolName,
            order.Provider?.ProviderName,
            order.OrderStatus.ToString(),
            latestPayment?.TransactionStatus.ToString() ?? "NoPayment",
            order.TotalAmount,
            order.CreatedAt,
            paidAt
        );
    }

    private static decimal CalculateRate(int count, int total)
    {
        return total > 0 ? Math.Round(count * 100m / total, 2) : 0;
    }
}
