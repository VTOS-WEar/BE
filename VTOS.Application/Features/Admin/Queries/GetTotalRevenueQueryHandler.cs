using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class GetTotalRevenueQueryHandler : IGetTotalRevenueQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetTotalRevenueQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TotalRevenueReportDto> HandleAsync(
        GetTotalRevenueQuery query,
        CancellationToken cancellationToken)
    {
        var ordersQuery = _context.Orders.AsQueryable();

        if (query.DateFrom.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= query.DateTo.Value);

        var totalRevenue = await ordersQuery
            .Where(o => o.OrderStatus == OrderStatus.Delivered)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        var DeliveredPayments = await ordersQuery
            .Where(o => o.OrderStatus == OrderStatus.Delivered)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        var failedPayments = await ordersQuery
            .Where(o => o.OrderStatus == OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        // Revenue by school
        var revenueBySchoolTemp = await _context.Orders
            .Where(o => o.OrderStatus == OrderStatus.Delivered)
            .Include(o => o.ChildProfile)
                .ThenInclude(cp => cp.School)
            .GroupBy(o => new { SchoolId = o.ChildProfile.School.Id, SchoolName = o.ChildProfile.School.SchoolName })
            .Select(g => new { g.Key.SchoolId, g.Key.SchoolName, Revenue = g.Sum(o => o.TotalAmount), OrderCount = g.Count() })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(cancellationToken);

        var revenueBySchool = revenueBySchoolTemp
            .Select(x => new RevenueBySchoolDto(x.SchoolId, x.SchoolName, x.Revenue, x.OrderCount))
            .ToList();

        // Revenue by month
        var revenueByMonthTemp = await _context.Orders
            .Where(o => o.OrderStatus == OrderStatus.Delivered)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        var revenueByMonth = revenueByMonthTemp
            .Select(x => new RevenueByMonthDto($"{x.Year}-{x.Month:D2}", x.Revenue, x.Count))
            .ToList();

        // Revenue by campaign - needs campaign implementation
        var revenueByCampaign = new List<RevenueByCampaignDto>();

        return new TotalRevenueReportDto(
            totalRevenue,
            DeliveredPayments,
            failedPayments,
            revenueBySchool,
            revenueByMonth,
            revenueByCampaign
        );
    }
}
