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
            return new ParentFeedbacksResponse(new(), 0, query.Page, query.PageSize, new(), new());

        // Get all order items for parent's orders with their campaign/marketplace and outfit info
        var parentOrderItems = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Campaign)
            .Include(oi => oi.Order)
                .ThenInclude(o => o.SemesterPublication)
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Provider)
            .Include(oi => oi.ProductVariant)
                .ThenInclude(pv => pv.Outfit)
            .Where(oi => parentOrderIds.Contains(oi.OrderID))
            .Select(oi => new
            {
                OrderItemId = oi.Id,
                CampaignId = oi.Order.CampaignID,
                CampaignName = oi.Order.Campaign != null ? oi.Order.Campaign.CampaignName : null,
                SemesterPublicationId = oi.Order.SemesterPublicationID,
                SemesterName = oi.Order.SemesterPublication != null ? (oi.Order.SemesterPublication.Semester + " " + oi.Order.SemesterPublication.AcademicYear) : null,
                ProviderName = oi.Order.Provider != null ? oi.Order.Provider.ProviderName : null,
                OutfitId = oi.ProductVariant.OutfitID,
                OutfitName = oi.ProductVariant.Outfit.OutfitName,
                OutfitImage = oi.ProductVariant.VariantImageURL ?? oi.ProductVariant.Outfit.MainImageURL,
                OutfitType = oi.ProductVariant.Outfit.OutfitType.ToString(),
                Price = oi.UnitPrice,
                Size = oi.ProductVariant.Size,
                Quantity = oi.Quantity,
                OrderDate = oi.Order.OrderDate
            })
            .ToListAsync(ct);

        if (!parentOrderItems.Any())
            return new ParentFeedbacksResponse(new(), 0, query.Page, query.PageSize, new(), new());

        var orderItemIds = parentOrderItems.Select(oi => oi.OrderItemId).ToList();

        // Get feedbacks for these order items
        var feedbacks = await _db.Feedbacks
            .AsNoTracking()
            .Where(f => f.UserID == query.ParentId
                && orderItemIds.Contains(f.OrderItemID))
            .ToListAsync(ct);

        // Build the complete list to calculate filters and counts correctly
        var baseItems = new List<ParentFeedbackDto>();

        foreach (var oi in parentOrderItems)
        {
            // Find feedback for this order item
            var feedback = feedbacks.FirstOrDefault(f => f.OrderItemID == oi.OrderItemId);

            baseItems.Add(new ParentFeedbackDto(
                FeedbackId: feedback?.Id ?? Guid.Empty,
                OrderItemId: oi.OrderItemId,
                CampaignId: oi.CampaignId ?? oi.SemesterPublicationId ?? Guid.Empty,
                CampaignName: oi.CampaignName ?? oi.SemesterName ?? "Danh mục học kỳ",
                OutfitId: oi.OutfitId,
                OutfitName: oi.OutfitName,
                OutfitImageUrl: oi.OutfitImage ?? "",
                Rating: feedback?.Rating,
                Comment: feedback?.Comment,
                FeedbackTimestamp: feedback?.Timestamp,
                OrderDate: oi.OrderDate,
                OutfitPrice: oi.Price,
                OutfitType: oi.OutfitType,
                Size: oi.Size ?? "N/A",
                Quantity: oi.Quantity,
                ProviderName: oi.ProviderName
            ));
        }

        // 1. Calculate Campaign/Semester Filters
        var campaignFilters = baseItems
            .GroupBy(f => new { f.CampaignId, f.CampaignName })
            .Select(g => new CampaignFilterDto(g.Key.CampaignId, g.Key.CampaignName, g.Count()))
            .OrderBy(c => c.CampaignName)
            .ToList();

        // 2. Filter by current campaignId if provided
        var itemsAfterCampaign = baseItems;
        if (query.CampaignId.HasValue)
        {
            itemsAfterCampaign = baseItems.Where(i => i.CampaignId == query.CampaignId.Value).ToList();
        }

        // 3. Calculate status counts
        var allCount = itemsAfterCampaign.Count;
        var ratedCount = itemsAfterCampaign.Count(f => f.Rating.HasValue);
        var notRatedCount = itemsAfterCampaign.Count(f => !f.Rating.HasValue);
        var ratingCounts = new List<RatingCountDto>
        {
            new RatingCountDto("all", allCount),
            new RatingCountDto("rated", ratedCount),
            new RatingCountDto("not-rated", notRatedCount)
        };

        // 4. Finally apply the hasRating filter for the main list
        var filteredItems = itemsAfterCampaign;
        if (query.HasRating.HasValue)
        {
            filteredItems = itemsAfterCampaign.Where(i => i.Rating.HasValue == query.HasRating.Value).ToList();
        }

        var total = filteredItems.Count;

        // Apply pagination
        var paginated = filteredItems
            .OrderByDescending(f => f.FeedbackTimestamp ?? DateTime.MinValue)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new ParentFeedbacksResponse(paginated, total, query.Page, query.PageSize, campaignFilters, ratingCounts);
    }
}
