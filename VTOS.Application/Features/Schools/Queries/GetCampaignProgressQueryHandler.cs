using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-46: Track pre-order progress for a specific campaign.
/// </summary>
public class GetCampaignProgressQueryHandler : IGetCampaignProgressQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetCampaignProgressQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CampaignProgressDto>> HandleAsync(GetCampaignProgressQuery query, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.SchoolID == query.SchoolId, ct);

        if (campaign == null)
            return Result<CampaignProgressDto>.Failure("Campaign not found or does not belong to this school.", "CAMPAIGN_NOT_FOUND");

        // Get orders for this campaign
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CampaignID == campaign.Id)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
            .ToListAsync(ct);

        var totalOrders = orders.Count;
        var totalRevenue = orders.Sum(o => o.TotalAmount);

        // Get outfit breakdown from CampaignOutfits
        var campaignOutfits = await _db.CampaignOutfits
            .AsNoTracking()
            .Where(co => co.CampaignID == campaign.Id)
            .Include(co => co.Outfit)
            .ToListAsync(ct);

        var outfitBreakdown = campaignOutfits.Select(co =>
        {
            // Count quantity ordered for this outfit across all orders
            var orderedQty = orders
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.ProductVariant != null && campaignOutfits.Any(x => x.OutfitID == co.OutfitID))
                .Sum(oi => oi.Quantity);

            var outfitRevenue = orders
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.ProductVariant != null && campaignOutfits.Any(x => x.OutfitID == co.OutfitID))
                .Sum(oi => oi.Quantity * oi.UnitPrice);

            return new OutfitBreakdownDto
            {
                OutfitId = co.OutfitID,
                OutfitName = co.Outfit.OutfitName,
                QuantityOrdered = orderedQty,
                MaxQuantity = co.MaxQuantity,
                Revenue = outfitRevenue
            };
        }).ToList();

        var dto = new CampaignProgressDto
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.CampaignName,
            Status = campaign.Status.ToString(),
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            OutfitBreakdown = outfitBreakdown
        };

        return Result<CampaignProgressDto>.Success(dto);
    }
}
