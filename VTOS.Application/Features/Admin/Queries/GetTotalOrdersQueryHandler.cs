using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class GetTotalOrdersQueryHandler : IGetTotalOrdersQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetTotalOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TotalOrdersReportDto> HandleAsync(
        GetTotalOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var ordersQuery = _context.Orders.AsQueryable();

        if (query.DateFrom.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= query.DateTo.Value);

        var orders = await ordersQuery.ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var completedOrders = orders.Count(o => o.OrderStatus == OrderStatus.Delivered);
        var pendingOrders = orders.Count(o => o.OrderStatus == OrderStatus.Pending);
        var cancelledOrders = orders.Count(o => o.OrderStatus == OrderStatus.Cancelled);

        // Orders by status
        var ordersByStatus = new[]
        {
            new OrderByStatusDto(
                Status: "Completed",
                Count: completedOrders,
                Percentage: totalOrders > 0 ? (completedOrders * 100m / totalOrders) : 0
            ),
            new OrderByStatusDto(
                Status: "Pending",
                Count: pendingOrders,
                Percentage: totalOrders > 0 ? (pendingOrders * 100m / totalOrders) : 0
            ),
            new OrderByStatusDto(
                Status: "Cancelled",
                Count: cancelledOrders,
                Percentage: totalOrders > 0 ? (cancelledOrders * 100m / totalOrders) : 0
            )
        }.ToList();

        // Orders by school
        var ordersBySchool = await _context.Orders
            .Include(o => o.ChildProfile)
                .ThenInclude(cp => cp.School)
            .GroupBy(o => new { SchoolId = o.ChildProfile.School.Id, SchoolName = o.ChildProfile.School.SchoolName })
            .Select(g => new { g.Key.SchoolId, g.Key.SchoolName, OrderCount = g.Count(), TotalAmount = g.Sum(o => o.TotalAmount) })
            .ToListAsync(cancellationToken);

        var ordersBySchoolDto = ordersBySchool
            .Select(x => new OrderBySchoolDto(x.SchoolId, x.SchoolName, x.OrderCount, x.TotalAmount))
            .OrderByDescending(x => x.OrderCount)
            .ToList();

        // Orders by month
        var ordersByMonth = orders
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Select(x => new OrderByMonthDto(
                Month: $"{x.Year}-{x.Month:D2}",
                OrderCount: x.Count,
                TotalAmount: x.Total
            ))
            .ToList();

        return new TotalOrdersReportDto(
            totalOrders,
            completedOrders,
            pendingOrders,
            cancelledOrders,
            ordersByStatus,
            ordersBySchoolDto,
            ordersByMonth
        );
    }
}
