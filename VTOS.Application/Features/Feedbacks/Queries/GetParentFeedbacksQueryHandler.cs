using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Feedbacks.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Feedbacks.Queries;

public interface IGetParentFeedbacksQueryHandler
{
    Task<ParentFeedbacksResponse> HandleAsync(GetParentFeedbacksQuery query, CancellationToken ct = default);
}

public class GetParentFeedbacksQueryHandler : IGetParentFeedbacksQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetParentFeedbacksQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ParentFeedbacksResponse> HandleAsync(GetParentFeedbacksQuery query, CancellationToken ct = default)
    {
        // Get all delivered orders for this parent
        var parentOrderIds = await _db.Orders.Include(x => x.ChildProfile)
            .AsNoTracking()
            .Where(o => o.ChildProfile.ParentUserID == query.ParentId && o.OrderStatus == OrderStatus.Delivered)
            .Select(o => o.Id)
            .ToListAsync(ct);

        if (!parentOrderIds.Any())
            return new ParentFeedbacksResponse(new(), 0, query.Page, query.PageSize, new());

        // Get all order items for parent's orders with their campaign and outfit info
        var parentOrderItems = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Campaign)
            .Include(oi => oi.ProductVariant)
                .ThenInclude(pv => pv.Outfit)
            .Where(oi => parentOrderIds.Contains(oi.OrderID))
            .Select(oi => new
            {
                OrderItemId = oi.Id,
                CampaignId = oi.Order.CampaignID,
                CampaignName = oi.Order.Campaign!.CampaignName,
                OutfitId = oi.ProductVariant.OutfitID,
                OutfitName = oi.ProductVariant.Outfit.OutfitName,
                OutfitImage = oi.ProductVariant.VariantImageURL ?? oi.ProductVariant.Outfit.MainImageURL,
                OutfitType = oi.ProductVariant.Outfit.OutfitType.ToString(),
                Price = oi.UnitPrice
            })
            .ToListAsync(ct);

        if (!parentOrderItems.Any())
            return new ParentFeedbacksResponse(new(), 0, query.Page, query.PageSize, new());

        var orderItemIds = parentOrderItems.Select(oi => oi.OrderItemId).ToList();

        // Get feedbacks for these order items
        var feedbacks = await _db.Feedbacks
            .AsNoTracking()
            .Where(f => f.UserID == query.ParentId
                && orderItemIds.Contains(f.OrderItemID))
            .ToListAsync(ct);

        // Build the complete list
        var allFeedbackItems = new List<ParentFeedbackDto>();

        foreach (var oi in parentOrderItems)
        {
            // Apply campaign filter
            if (query.CampaignId.HasValue && query.CampaignId != oi.CampaignId)
                continue;

            // Find feedback for this order item
            var feedback = feedbacks.FirstOrDefault(f => f.OrderItemID == oi.OrderItemId);

            // Apply rating filter
            if (query.HasRating.HasValue)
            {
                if (query.HasRating.Value && feedback == null)
                    continue;
                if (!query.HasRating.Value && feedback != null)
                    continue;
            }

            allFeedbackItems.Add(new ParentFeedbackDto(
                FeedbackId: feedback?.Id ?? Guid.Empty,
                OrderItemId: oi.OrderItemId,
                CampaignId: oi.CampaignId ?? Guid.Empty,
                CampaignName: oi.CampaignName ?? "Unknown Campaign",
                OutfitId: oi.OutfitId,
                OutfitName: oi.OutfitName,
                OutfitImageUrl: oi.OutfitImage ?? "",
                Rating: feedback?.Rating,
                Comment: feedback?.Comment,
                FeedbackTimestamp: feedback?.Timestamp,
                OutfitPrice: oi.Price,
                OutfitType: oi.OutfitType
            ));
        }

        // Get total before pagination
        var total = allFeedbackItems.Count;

        // Order by timestamp (newest first for rated, any for not-rated)
        var ordered = allFeedbackItems
            .OrderByDescending(f => f.FeedbackTimestamp ?? DateTime.MinValue)
            .ToList();

        // Apply pagination
        var paginated = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Get campaign filter options
        var campaignFilters = ordered
            .GroupBy(f => new { f.CampaignId, f.CampaignName })
            .Select(g => new CampaignFilterDto(g.Key.CampaignId, g.Key.CampaignName, g.Count()))
            .OrderBy(c => c.CampaignName)
            .ToList();

        return new ParentFeedbacksResponse(paginated, total, query.Page, query.PageSize, campaignFilters);
    }
}
