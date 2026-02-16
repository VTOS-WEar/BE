using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-49: View sales reports for the school.
/// Aggregates order data grouped by month and top-selling outfits.
/// </summary>
public class GetSalesReportQueryHandler : IGetSalesReportQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSalesReportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SalesReportDto>> HandleAsync(GetSalesReportQuery query, CancellationToken ct = default)
    {
        // Get all orders related to this school
        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
            .Where(o =>
                (o.Campaign != null && o.Campaign.SchoolID == query.SchoolId) ||
                o.ChildProfile.SchoolID == query.SchoolId
            );

        // Apply date filters
        if (query.FromDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.OrderDate <= query.ToDate.Value);

        var orders = await ordersQuery.ToListAsync(ct);

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalOrders = orders.Count;
        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        // Monthly sales breakdown
        var monthlySales = orders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new MonthlySalesDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month)
            .ToList();

        // Top outfits — get outfit IDs for this school
        var schoolOutfitIds = await _db.Outfits
            .AsNoTracking()
            .Where(o => o.SchoolID == query.SchoolId)
            .Select(o => o.Id)
            .ToListAsync(ct);

        var topOutfits = orders
            .SelectMany(o => o.OrderItems)
            .Where(oi => oi.ProductVariant != null)
            .GroupBy(oi => oi.ProductVariant.OutfitID)
            .Where(g => schoolOutfitIds.Contains(g.Key))
            .Select(g => new TopOutfitDto
            {
                OutfitId = g.Key,
                TotalSold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
            })
            .OrderByDescending(t => t.TotalSold)
            .Take(10)
            .ToList();

        // Fill outfit names
        var outfitIds = topOutfits.Select(t => t.OutfitId).ToList();
        var outfitNames = await _db.Outfits
            .AsNoTracking()
            .Where(o => outfitIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.OutfitName, ct);

        foreach (var top in topOutfits)
        {
            if (outfitNames.TryGetValue(top.OutfitId, out var name))
                top.OutfitName = name;
        }

        return Result<SalesReportDto>.Success(new SalesReportDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            AvgOrderValue = avgOrderValue,
            MonthlySales = monthlySales,
            TopOutfits = topOutfits
        });
    }
}
