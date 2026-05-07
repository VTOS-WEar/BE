using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Queries;

public class GetOrderHistoryQueryHandler : IGetOrderHistoryQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetOrderHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OrderHistoryResponse>> HandleAsync(
        GetOrderHistoryQuery query, CancellationToken cancellationToken = default)
    {
        // Normalize pagination
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        // Base query: orders belonging to this parent (via ChildProfile)
        var ordersQuery = _context.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile)
                .ThenInclude(cp => cp.School)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Outfit)
            .Include(o => o.PaymentTransactions)
            .Where(o => o.ChildProfile.ParentUserID == query.ParentId);

        // Apply filters
        ordersQuery = ApplyFilters(ordersQuery, query);

        // Get total count before pagination
        var totalCount = await ordersQuery.CountAsync(cancellationToken);

        // Apply sorting
        ordersQuery = ApplySorting(ordersQuery, query.SortBy, query.SortOrder);

        // Apply pagination
        var orders = await ordersQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map to response
        var items = orders.Select(o =>
        {
            var latestTransaction = o.PaymentTransactions
                .OrderByDescending(t => t.TransactionTimestamp)
                .FirstOrDefault();

            return new OrderHistoryItem
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus,
                OrderStatusName = o.OrderStatus.ToString(),
                TotalAmount = o.TotalAmount,
                ShippingFee = o.ShippingFee,
                ShippingAddress = o.ShippingAddress,
                DeliveryMethod = o.DeliveryMethod,
                ItemCount = o.OrderItems.Count,
                PaymentStatusName = latestTransaction?.TransactionStatus.ToString(),
                ChildName = o.ChildProfile.FullName,
                FirstItemImageUrl = o.OrderItems
                    .Select(oi => oi.ProductVariant?.VariantImageURL ?? oi.ProductVariant?.Outfit?.MainImageURL)
                    .FirstOrDefault(),
                SchoolName = o.ChildProfile.School?.SchoolName
            };
        }).ToList();

        var response = new OrderHistoryResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return Result<OrderHistoryResponse>.Success(response);
    }

    private static IQueryable<Order> ApplyFilters(IQueryable<Order> query, GetOrderHistoryQuery filter)
    {
        // Filter by status
        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.OrderStatus == filter.Status.Value);
        }

        // Filter by date range
        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            var toDateEnd = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(o => o.OrderDate < toDateEnd);
        }

        // Search by shipping address
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.Trim();
            query = query.Where(o => o.ShippingAddress.Contains(searchTerm));
        }

        return query;
    }

    private static IQueryable<Order> ApplySorting(IQueryable<Order> query, string? sortBy, string? sortOrder)
    {
        var isAscending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLowerInvariant() switch
        {
            "totalamount" => isAscending
                ? query.OrderBy(o => o.TotalAmount)
                : query.OrderByDescending(o => o.TotalAmount),
            "status" => isAscending
                ? query.OrderBy(o => o.OrderStatus)
                : query.OrderByDescending(o => o.OrderStatus),
            _ => isAscending
                ? query.OrderBy(o => o.OrderDate)
                : query.OrderByDescending(o => o.OrderDate)
        };
    }
}
