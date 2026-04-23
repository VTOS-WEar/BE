using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Queries;

public class GetOrderStatusQueryHandler : IGetOrderStatusQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetOrderStatusQueryHandler> _logger;

    public GetOrderStatusQueryHandler(
        IApplicationDbContext context,
        ILogger<GetOrderStatusQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<OrderStatusResponse>> HandleAsync(
        GetOrderStatusQuery query, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile)
                .ThenInclude(cp => cp.School)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Outfit)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == query.OrderId, cancellationToken);

        if (order == null)
            return Result<OrderStatusResponse>.Failure("Order not found", "ORDER_NOT_FOUND");

        // Validate ownership
        if (order.ChildProfile.ParentUserID != query.ParentId)
        {
            _logger.LogWarning("Unauthorized access: Parent {ParentId} on Order {OrderId}",
                query.ParentId, query.OrderId);
            return Result<OrderStatusResponse>.Failure("You are not authorized to view this order", "UNAUTHORIZED_ORDER_ACCESS");
        }

        // Get latest payment transaction status
        var latestTransaction = order.PaymentTransactions
            .OrderByDescending(t => t.TransactionTimestamp)
            .FirstOrDefault();

        var response = new OrderStatusResponse
        {
            OrderId = order.Id,
            OrderStatus = order.OrderStatus,
            OrderStatusName = order.OrderStatus.ToString(),
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            DeliveryMethod = order.DeliveryMethod,
            PaymentStatus = latestTransaction?.TransactionStatus,
            PaymentStatusName = latestTransaction?.TransactionStatus.ToString(),
            Items = order.OrderItems.Select(oi => new OrderItemDetail
            {
                ProductVariantId = oi.ProductVariantID,
                ProductName = oi.ProductVariant?.Outfit.OutfitName ?? "Unknown Product",
                SKUCode = oi.ProductVariant?.SKUCode,
                Size = oi.SizeOrdered,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                ImageUrl = oi.ProductVariant?.VariantImageURL ?? oi.ProductVariant?.Outfit?.MainImageURL,
                SchoolName = order.ChildProfile.School?.SchoolName
            }).ToList()
        };

        return Result<OrderStatusResponse>.Success(response);
    }
}
