using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Feedbacks.DTOs;

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
        // Get all orders for this parent
        var parentOrderIds = await _db.Orders.Include(x => x.ChildProfile)
            .AsNoTracking()
            .Where(o => o.ChildProfile.ParentUserID == query.ParentId)
            .Select(o => o.Id)
            .ToListAsync(ct);

        if (!parentOrderIds.Any())
            return new ParentFeedbacksResponse(new(), 0, query.Page, query.PageSize, new());

        // Get all product variants ordered by parent with their outfit info
        var parentProductVariants = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.ProductVariant)
                .ThenInclude(pv => pv.Outfit)
            .Where(oi => parentOrderIds.Contains(oi.OrderID))
            .Select(oi => new
            {
                ProductVariantId = oi.ProductVariantID,
                Size = oi.ProductVariant.Size,
                ColorVariant = oi.ProductVariant.ColorVariant,
                OutfitId = oi.ProductVariant.OutfitID,
                OutfitName = oi.ProductVariant.Outfit.OutfitName,
                OutfitImage = oi.ProductVariant.VariantImageURL ?? oi.ProductVariant.Outfit.MainImageURL,
                OutfitType = oi.ProductVariant.Outfit.OutfitType.ToString()
            })
            .Distinct()
            .ToListAsync(ct);

        if (!parentProductVariants.Any())
            return new ParentFeedbacksResponse(new(), 0, query.Page, query.PageSize, new());

        var productVariantIds = parentProductVariants.Select(pv => pv.ProductVariantId).Distinct().ToList();
        var outfitIds = parentProductVariants.Select(pv => pv.OutfitId).Distinct().ToList();

        // Get campaign outfits for these outfits (for pricing info)
        var campaignOutfitPrices = await _db.CampaignOutfits
            .AsNoTracking()
            .Where(co => outfitIds.Contains(co.OutfitID))
            .Select(co => new
            {
                co.Id,
                co.OutfitID,
                co.CampaignID,
                co.CampaignPrice,
                CampaignName = co.Campaign!.CampaignName
            })
            .ToListAsync(ct);

        if (!campaignOutfitPrices.Any())
            return new ParentFeedbacksResponse(new(), 0, query.Page, query.PageSize, new());

        var campaignIds = campaignOutfitPrices.Select(co => co.CampaignID).Distinct().ToList();

        // Get feedbacks for this parent with these product variants
        var feedbacks = await _db.Feedbacks
            .AsNoTracking()
            .Where(f => f.UserID == query.ParentId
                && productVariantIds.Contains(f.ProductVariantID)
                && campaignIds.Contains(f.CampaignID))
            .ToListAsync(ct);

        // Build the complete list - grouping by ProductVariant + Campaign
        var allFeedbackItems = new List<ParentFeedbackDto>();

        foreach (var pv in parentProductVariants)
        {
            foreach (var co in campaignOutfitPrices.Where(c => c.OutfitID == pv.OutfitId))
            {
                // Find feedback for this ProductVariant + Campaign
                var feedback = feedbacks.FirstOrDefault(f => 
                    f.ProductVariantID == pv.ProductVariantId && f.CampaignID == co.CampaignID);

                // Apply campaign filter
                if (query.CampaignId.HasValue && query.CampaignId != co.CampaignID)
                    continue;

                // Apply rating filter
                if (query.HasRating.HasValue)
                {
                    if (query.HasRating.Value && feedback == null)
                        continue;
                    if (!query.HasRating.Value && feedback != null)
                        continue;
                }

                // Build product variant display name with size
                var productVariantName = $"{pv.OutfitName}" + 
                    (string.IsNullOrEmpty(pv.Size) ? "" : $" (Size: {pv.Size})") +
                    (string.IsNullOrEmpty(pv.ColorVariant) ? "" : $" - {pv.ColorVariant}");

                allFeedbackItems.Add(new ParentFeedbackDto(
                    FeedbackId: feedback?.Id ?? Guid.Empty,
                    CampaignOutfitId: co.Id,
                    CampaignId: co.CampaignID,
                    CampaignName: co.CampaignName,
                    OutfitId: pv.ProductVariantId,
                    OutfitName: productVariantName,
                    OutfitImageUrl: pv.OutfitImage,
                    Rating: feedback?.Rating,
                    Comment: feedback?.Comment,
                    FeedbackTimestamp: feedback?.Timestamp,
                    OutfitPrice: co.CampaignPrice,
                    OutfitType: pv.OutfitType
                ));
            }
        }

        // Remove duplicates (same product variant + campaign)
        allFeedbackItems = allFeedbackItems
            .GroupBy(f => new { f.OutfitId, f.CampaignId })
            .Select(g => g.First())
            .ToList();

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
