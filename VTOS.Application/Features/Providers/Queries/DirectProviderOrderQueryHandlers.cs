using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderIncomingOrdersQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public interface IGetProviderIncomingOrdersQueryHandler
{
    Task<Result<ProviderIncomingOrdersResponse>> HandleAsync(GetProviderIncomingOrdersQuery query, CancellationToken cancellationToken = default);
}

public record GetProviderDirectOrderDetailQuery(Guid UserId, Guid OrderId);

public interface IGetProviderDirectOrderDetailQueryHandler
{
    Task<Result<ProviderDirectOrderDetailDto>> HandleAsync(GetProviderDirectOrderDetailQuery query, CancellationToken cancellationToken = default);
}

public record GetProviderOrderStatsQuery(Guid UserId);

public interface IGetProviderOrderStatsQueryHandler
{
    Task<Result<ProviderOrderStatsDto>> HandleAsync(GetProviderOrderStatsQuery query, CancellationToken cancellationToken = default);
}

public class GetProviderIncomingOrdersQueryHandler : IGetProviderIncomingOrdersQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderIncomingOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProviderIncomingOrdersResponse>> HandleAsync(GetProviderIncomingOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var providerId = await ResolveProviderIdAsync(query.UserId, cancellationToken);
        if (!providerId.HasValue)
            return Result<ProviderIncomingOrdersResponse>.Failure("Provider not found for current user.", "PROVIDER_NOT_FOUND");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var ordersQuery = _context.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile)
                .ThenInclude(cp => cp.ParentUser)
            .Include(o => o.OrderItems)
            .Where(o => o.ProviderID == providerId.Value && o.SemesterPublicationID != null);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, true, out var parsedStatus))
            ordersQuery = ordersQuery.Where(o => o.OrderStatus == parsedStatus);

        var totalCount = await ordersQuery.CountAsync(cancellationToken);
        var orders = await ordersQuery
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<ProviderIncomingOrdersResponse>.Success(new ProviderIncomingOrdersResponse
        {
            Items = orders.Select(o => new ProviderIncomingOrderItemDto
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus.ToString(),
                TotalAmount = o.TotalAmount,
                ParentName = o.ChildProfile.ParentUser?.FullName ?? string.Empty,
                ChildName = o.ChildProfile.FullName,
                ItemCount = o.OrderItems.Count,
                TrackingCode = o.TrackingCode
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    private async Task<Guid?> ResolveProviderIdAsync(Guid userId, CancellationToken ct)
    {
        return await _context.ProviderManagers
            .AsNoTracking()
            .Where(pm => pm.UserID == userId)
            .Select(pm => (Guid?)pm.ProviderID)
            .FirstOrDefaultAsync(ct);
    }
}

public class GetProviderDirectOrderDetailQueryHandler : IGetProviderDirectOrderDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderDirectOrderDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProviderDirectOrderDetailDto>> HandleAsync(GetProviderDirectOrderDetailQuery query, CancellationToken cancellationToken = default)
    {
        var providerId = await _context.ProviderManagers
            .AsNoTracking()
            .Where(pm => pm.UserID == query.UserId)
            .Select(pm => (Guid?)pm.ProviderID)
            .FirstOrDefaultAsync(cancellationToken);

        if (!providerId.HasValue)
            return Result<ProviderDirectOrderDetailDto>.Failure("Provider not found for current user.", "PROVIDER_NOT_FOUND");

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile)
                .ThenInclude(cp => cp.ParentUser)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(v => v.Outfit)
            .FirstOrDefaultAsync(o => o.Id == query.OrderId && o.ProviderID == providerId.Value && o.SemesterPublicationID != null, cancellationToken);

        if (order == null)
            return Result<ProviderDirectOrderDetailDto>.Failure("Direct order not found.", "ORDER_NOT_FOUND");

        return Result<ProviderDirectOrderDetailDto>.Success(new ProviderDirectOrderDetailDto
        {
            OrderId = order.Id,
            OrderDate = order.OrderDate,
            OrderStatus = order.OrderStatus.ToString(),
            PricingMode = order.AppliedPricingMode?.ToString() ?? string.Empty,
            TotalAmount = order.TotalAmount,
            ParentName = order.ChildProfile.ParentUser?.FullName ?? string.Empty,
            ParentPhone = order.ChildProfile.ParentUser?.Phone,
            ChildName = order.ChildProfile.FullName,
            ShippingAddress = order.ShippingAddress,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            DeliveryMethod = order.DeliveryMethod,
            TrackingCode = order.TrackingCode,
            ShippingCompany = order.ShippingCompany,
            Items = order.OrderItems.Select(oi => new ProviderDirectOrderDetailItemDto
            {
                OrderItemId = oi.Id,
                OutfitName = oi.ProductVariant.Outfit.OutfitName,
                ImageUrl = oi.ProductVariant.Outfit.MainImageURL,
                Size = oi.SizeOrdered,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        });
    }
}

public class GetProviderOrderStatsQueryHandler : IGetProviderOrderStatsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderOrderStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProviderOrderStatsDto>> HandleAsync(GetProviderOrderStatsQuery query, CancellationToken cancellationToken = default)
    {
        var providerId = await _context.ProviderManagers
            .AsNoTracking()
            .Where(pm => pm.UserID == query.UserId)
            .Select(pm => (Guid?)pm.ProviderID)
            .FirstOrDefaultAsync(cancellationToken);

        if (!providerId.HasValue)
            return Result<ProviderOrderStatsDto>.Failure("Provider not found for current user.", "PROVIDER_NOT_FOUND");

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.ProviderID == providerId.Value && o.SemesterPublicationID != null)
            .ToListAsync(cancellationToken);

        return Result<ProviderOrderStatsDto>.Success(new ProviderOrderStatsDto
        {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.OrderStatus == OrderStatus.Pending),
            PaidOrders = orders.Count(o => o.OrderStatus == OrderStatus.Paid),
            InProgressOrders = orders.Count(o => o.OrderStatus == OrderStatus.Accepted || o.OrderStatus == OrderStatus.InProduction || o.OrderStatus == OrderStatus.ReadyToShip),
            CompletedShipmentOrders = orders.Count(o => o.OrderStatus == OrderStatus.Shipped || o.OrderStatus == OrderStatus.Delivered),
            TotalRevenue = orders.Where(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderStatus != OrderStatus.Refunded).Sum(o => o.TotalAmount),
            StatusCounts = orders.GroupBy(o => o.OrderStatus.ToString()).ToDictionary(g => g.Key, g => g.Count())
        });
    }
}
