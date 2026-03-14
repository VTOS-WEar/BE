using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class ViewReportQueryHandler : IViewReportQueryHandler
{
    private readonly IApplicationDbContext _context;

    public ViewReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<dynamic> HandleAsync(
        ViewReportQuery query,
        CancellationToken cancellationToken)
    {
        return query.ReportType.ToLower() switch
        {
            "order" => await GetOrderReport(query, cancellationToken),
            "revenue" => await GetRevenueReport(query, cancellationToken),
            "schoolperformance" => await GetSchoolPerformanceReport(query, cancellationToken),
            "providerperformance" => await GetProviderPerformanceReport(query, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown report type: {query.ReportType}")
        };
    }

    private async Task<dynamic> GetOrderReport(ViewReportQuery query, CancellationToken cancellationToken)
    {
        var ordersQuery = _context.Orders.AsQueryable();

        if (query.DateFrom.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= query.DateFrom.Value);
        if (query.DateTo.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= query.DateTo.Value);
        if (query.SchoolId.HasValue)
            ordersQuery = ordersQuery.Where(o => o.ChildProfile.School.Id == query.SchoolId.Value);

        var orders = await ordersQuery
            .Include(o => o.ChildProfile)
            .ThenInclude(cp => cp.School)
            .ToListAsync(cancellationToken);

        return new
        {
            ReportType = "Order",
            GeneratedAt = DateTime.UtcNow,
            DateRange = new { From = query.DateFrom, To = query.DateTo },
            Summary = new
            {
                TotalOrders = orders.Count,
                CompletedOrders = orders.Count(o => o.OrderStatus == OrderStatus.Delivered),
                PendingOrders = orders.Count(o => o.OrderStatus == OrderStatus.Pending),
                CancelledOrders = orders.Count(o => o.OrderStatus == OrderStatus.Cancelled),
                TotalRevenue = orders.Where(o => o.OrderStatus == OrderStatus.Delivered).Sum(o => o.TotalAmount)
            },
            Details = orders.GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToList()
        };
    }

    private async Task<dynamic> GetRevenueReport(ViewReportQuery query, CancellationToken cancellationToken)
    {
        var ordersQuery = _context.Orders
            .Where(o => o.OrderStatus == OrderStatus.Delivered)
            .AsQueryable();

        if (query.DateFrom.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= query.DateFrom.Value);
        if (query.DateTo.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= query.DateTo.Value);
        if (query.SchoolId.HasValue)
            ordersQuery = ordersQuery.Where(o => o.ChildProfile.School.Id == query.SchoolId.Value);

        var orders = await ordersQuery
            .Include(o => o.ChildProfile)
            .ThenInclude(cp => cp.School)
            .ToListAsync(cancellationToken);

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var revenueBySchool = orders
            .GroupBy(o => o.ChildProfile.School.SchoolName)
            .Select(g => new { School = g.Key, Revenue = g.Sum(o => o.TotalAmount), OrderCount = g.Count() })
            .ToList();

        return new
        {
            ReportType = "Revenue",
            GeneratedAt = DateTime.UtcNow,
            DateRange = new { From = query.DateFrom, To = query.DateTo },
            Summary = new { TotalRevenue = totalRevenue, OrderCount = orders.Count },
            BySchool = revenueBySchool
        };
    }

    private async Task<dynamic> GetSchoolPerformanceReport(ViewReportQuery query, CancellationToken cancellationToken)
    {
        var schoolsQuery = _context.Schools.AsQueryable();

        var schools = await schoolsQuery
            .Include(s => s.ChildProfiles)
            .ThenInclude(cp => cp.Orders)
            .Include(s => s.Campaigns)
            .ToListAsync(cancellationToken);

        var performanceData = schools.Select(s => new
        {
            SchoolName = s.SchoolName,
            TotalOrders = s.ChildProfiles.SelectMany(cp => cp.Orders).Count(),
            CompletedOrders = s.ChildProfiles.SelectMany(cp => cp.Orders).Count(o => o.OrderStatus == OrderStatus.Delivered),
            TotalRevenue = s.ChildProfiles.SelectMany(cp => cp.Orders).Where(o => o.OrderStatus == OrderStatus.Delivered).Sum(o => o.TotalAmount),
            ActiveCampaigns = s.Campaigns.Count(c => c.Status == CampaignStatus.Active)
        }).ToList();

        return new
        {
            ReportType = "SchoolPerformance",
            GeneratedAt = DateTime.UtcNow,
            SchoolCount = performanceData.Count,
            Data = performanceData
        };
    }

    private async Task<dynamic> GetProviderPerformanceReport(ViewReportQuery query, CancellationToken cancellationToken)
    {
        var providers = await _context.Providers
            .ToListAsync(cancellationToken);

        // Provider performance would include orders processed, production batches, etc.
        var performanceData = providers.Select(p => new
        {
            ProviderName = p.ProviderName,
            ContactEmail = p.Email,
            Status = p.Status ?? "Unknown"
        }).ToList();

        return new
        {
            ReportType = "ProviderPerformance",
            GeneratedAt = DateTime.UtcNow,
            ProviderCount = performanceData.Count,
            Data = performanceData
        };
    }
}
