using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;

namespace VTOS.Application.Features.Orders.Queries;

public class GetOrderDetailForFeedbackQueryHandler : IGetOrderDetailForFeedbackQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetOrderDetailForFeedbackQueryHandler> _logger;

    public GetOrderDetailForFeedbackQueryHandler(
        IApplicationDbContext context,
        ILogger<GetOrderDetailForFeedbackQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<OrderDetailForFeedbackDto>> HandleAsync(
        GetOrderDetailForFeedbackQuery query, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile)
            .Include(o => o.Campaign)
                .ThenInclude(c => c!.CampaignOutfits)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Outfit)
            .FirstOrDefaultAsync(o => o.Id == query.OrderId, cancellationToken);

        if (order == null)
            return Result<OrderDetailForFeedbackDto>.Failure("Order not found", "ORDER_NOT_FOUND");

        // Validate ownership
        if (order.ChildProfile.ParentUserID != query.ParentId)
        {
            _logger.LogWarning("Unauthorized access: Parent {ParentId} on Order {OrderId}",
                query.ParentId, query.OrderId);
            return Result<OrderDetailForFeedbackDto>.Failure("You are not authorized to view this order", "UNAUTHORIZED_ORDER_ACCESS");
        }

        // Build items with campaign outfit information
        var items = new List<OrderItemForFeedbackDto>();
        foreach (var oi in order.OrderItems)
        {
            if (oi.ProductVariant?.Outfit == null)
                continue;

            var outfit = oi.ProductVariant.Outfit;
            
            // Find the CampaignOutfit that corresponds to this order and outfit
            var campaignOutfit = order.Campaign?.CampaignOutfits
                .FirstOrDefault(co => co.OutfitID == outfit.Id);

            var campaignOutfitId = campaignOutfit?.Id ?? Guid.Empty;

            items.Add(new OrderItemForFeedbackDto
            {
                OrderItemId = oi.Id,
                CampaignOutfitId = campaignOutfitId,
                ProductVariantId = oi.ProductVariantID,
                CampaignId = order.CampaignID!.Value,
                OutfitId = outfit.Id,
                OutfitName = outfit.OutfitName,
                OutfitImage = oi.ProductVariant.VariantImageURL ?? outfit.MainImageURL,
                Quantity = oi.Quantity,
                Size = oi.SizeOrdered,
                Price = oi.UnitPrice
            });
        }

        var response = new OrderDetailForFeedbackDto
        {
            OrderId = order.Id,
            ChildProfileId = order.ChildProfileID,
            ChildName = order.ChildProfile.FullName,
            ChildAvatar = order.ChildProfile.Avatar,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus.ToString(),
            OrderDate = order.OrderDate,
            ShippingAddress = order.ShippingAddress,
            Items = items
        };

        return Result<OrderDetailForFeedbackDto>.Success(response);
    }
}
