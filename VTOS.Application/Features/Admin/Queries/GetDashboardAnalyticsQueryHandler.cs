using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class GetDashboardAnalyticsQueryHandler : IGetDashboardAnalyticsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetDashboardAnalyticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardAnalyticsDto> HandleAsync(
        GetDashboardAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        // Total counts
        var totalUsers = await _context.Users.Where(u => !u.IsDeleted).CountAsync(cancellationToken);
        var totalSchools = await _context.Schools.Where(s => !s.IsDeleted).CountAsync(cancellationToken);
        var totalProviders = await _context.Providers.Where(p => !p.IsDeleted).CountAsync(cancellationToken);
        var totalParents = await _context.Users
            .Where(u => !u.IsDeleted && u.Role.RoleName == "Parent")
            .CountAsync(cancellationToken);
        var totalOrders = await _context.Orders.Where(o => o.OrderStatus != OrderStatus.Cancelled).CountAsync(cancellationToken);
        var totalRevenue = await _context.Orders
            .Where(o => o.OrderStatus == OrderStatus.Delivered)
            .SumAsync(o => o.TotalAmount, cancellationToken);
        var pendingApprovals = await _context.AccountRequests
            .Where(a => a.Status == AccountRequestStatus.Pending)
            .CountAsync(cancellationToken);
        var pendingWithdrawals = await _context.WalletWithdrawalRequests
            .Where(w => w.Status == "Pending")
            .CountAsync(cancellationToken);

        // Get date range
        var now = DateTime.UtcNow;
        DateTime startDate = query.TimeRange switch
        {
            "Week" => now.AddDays(-7),
            "Year" => now.AddYears(-1),
            _ => now.AddMonths(-12) // Default: last 12 months
        };

        // Orders per month
        var ordersPerMonthTemp = await _context.Orders
            .Where(o => o.CreatedAt >= startDate && o.OrderStatus != OrderStatus.Cancelled)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        var ordersPerMonth = ordersPerMonthTemp
            .Select(x => new MonthlyOrderDto($"{x.Year}-{x.Month:D2}", x.Count))
            .ToList();

        // Revenue per month
        var revenuePerMonthTemp = await _context.Orders
            .Where(o => o.CreatedAt >= startDate && o.OrderStatus == OrderStatus.Delivered)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        var revenuePerMonth = revenuePerMonthTemp
            .Select(x => new MonthlyRevenueDto($"{x.Year}-{x.Month:D2}", x.Revenue))
            .ToList();

        // Users per month by role
        var usersPerMonthTemp = await _context.Users
            .Where(u => !u.IsDeleted && u.CreatedAt >= startDate)
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month, u.Role.RoleName })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Role = g.Key.RoleName,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Role)
            .ToListAsync(cancellationToken);

        var usersPerMonth = usersPerMonthTemp
            .Select(x => new MonthlyUserDto($"{x.Year}-{x.Month:D2}", x.Role, x.Count))
            .ToList();

        // System status breakdowns
        var orderStatusBreakdownTemp = await _context.Orders
            .GroupBy(o => o.OrderStatus)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var orderStatusBreakdown = orderStatusBreakdownTemp
            .Select(x => new StatusBreakdownDto(x.Status.ToString(), x.Count, x.TotalAmount))
            .ToList();

        var paymentStatusBreakdownTemp = await _context.PaymentTransactions
            .GroupBy(p => p.TransactionStatus)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var paymentStatusBreakdown = paymentStatusBreakdownTemp
            .Select(x => new StatusBreakdownDto(x.Status.ToString(), x.Count, x.TotalAmount))
            .ToList();

        // Top selling uniforms
        var topSellingTemp = await _context.OrderItems
            .Include(oi => oi.ProductVariant)
                .ThenInclude(pv => pv.Outfit)
            .GroupBy(oi => oi.ProductVariantID)
            .Select(g => new
            {
                VariantId = g.Key,
                OutfitId = g.First().ProductVariant.Outfit.Id,
                OutfitName = g.First().ProductVariant.Outfit.OutfitName,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(10)
            .ToListAsync(cancellationToken);

        var topSelling = topSellingTemp
            .Select(x => new TopSellingUniformDto(x.OutfitId, x.OutfitName, x.QuantitySold, x.Revenue))
            .ToList();

        return new DashboardAnalyticsDto(
            totalUsers,
            totalSchools,
            totalProviders,
            totalParents,
            totalOrders,
            totalRevenue,
            pendingApprovals,
            pendingWithdrawals,
            ordersPerMonth,
            revenuePerMonth,
            usersPerMonth,
            orderStatusBreakdown,
            paymentStatusBreakdown,
            topSelling
        );
    }
}
