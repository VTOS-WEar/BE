using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Enums;

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

        // Get orders for this campaign (exclude cancelled/refunded)
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CampaignID == campaign.Id
                && o.OrderStatus != OrderStatus.Cancelled
                && o.OrderStatus != OrderStatus.Refunded)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
            .ToListAsync(ct);

        var totalOrders = orders.Count;
        var totalRevenue = orders.Sum(o => o.TotalAmount);

        // Distinct students who placed orders
        var totalStudents = orders.Select(o => o.ChildProfileID).Distinct().Count();

        // Pending orders = Pending (chờ thanh toán) + Paid (chờ xác nhận)
        var pendingOrders = orders.Count(o =>
            o.OrderStatus == OrderStatus.Pending || o.OrderStatus == OrderStatus.Paid);

        // Total students in the school (for X/Y progress display)
        var totalChildProfiles = await _db.ChildProfiles
            .AsNoTracking()
            .CountAsync(c => c.SchoolID == query.SchoolId && !c.IsDeleted, ct);

        // Get outfit breakdown from CampaignOutfits
        var campaignOutfits = await _db.CampaignOutfits
            .AsNoTracking()
            .Where(co => co.CampaignID == campaign.Id)
            .Include(co => co.Outfit)
            .ToListAsync(ct);

        // Build a set of outfit IDs in this campaign for fast lookup
        var campaignOutfitIds = campaignOutfits.Select(co => co.OutfitID).ToHashSet();

        // Get all product variant IDs that belong to campaign outfits
        var variantToOutfit = await _db.ProductVariants
            .AsNoTracking()
            .Where(pv => campaignOutfitIds.Contains(pv.OutfitID))
            .Select(pv => new { pv.Id, pv.OutfitID })
            .ToListAsync(ct);

        var variantOutfitMap = variantToOutfit.ToDictionary(v => v.Id, v => v.OutfitID);

        // Flatten all order items
        var allOrderItems = orders.SelectMany(o => o.OrderItems).ToList();

        var outfitBreakdown = campaignOutfits.Select(co =>
        {
            // Count quantity ordered for this outfit via ProductVariant mapping
            var outfitItems = allOrderItems
                .Where(oi => variantOutfitMap.TryGetValue(oi.ProductVariantID, out var outfitId) && outfitId == co.OutfitID)
                .ToList();

            var orderedQty = outfitItems.Sum(oi => oi.Quantity);
            var outfitRevenue = outfitItems.Sum(oi => oi.Quantity * oi.UnitPrice);

            return new OutfitBreakdownDto
            {
                OutfitId = co.OutfitID,
                OutfitName = co.Outfit.OutfitName,
                QuantityOrdered = orderedQty,
                MaxQuantity = co.MaxQuantity,
                Revenue = outfitRevenue,
                Category = co.Outfit.OutfitType.ToString()
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
            TotalStudents = totalStudents,
            PendingOrders = pendingOrders,
            TotalChildProfiles = totalChildProfiles,
            OutfitBreakdown = outfitBreakdown
        };

        return Result<CampaignProgressDto>.Success(dto);
    }
}

