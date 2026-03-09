using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetCampaignOrderedItemsQuery(Guid UserId, Guid CampaignId);

public record OrderedVariantDto(string Size, string? Color, int Quantity);

public record CampaignOrderedItemDto(
    Guid OutfitId,
    string OutfitName,
    string? MainImageUrl,
    int TotalQuantity,
    decimal TotalRevenue,
    IReadOnlyList<OrderedVariantDto> Variants
);

public interface IGetCampaignOrderedItemsQueryHandler
{
    Task<Result<IReadOnlyList<CampaignOrderedItemDto>>> HandleAsync(GetCampaignOrderedItemsQuery query, CancellationToken ct = default);
}

public class GetCampaignOrderedItemsQueryHandler : IGetCampaignOrderedItemsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetCampaignOrderedItemsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CampaignOrderedItemDto>>> HandleAsync(GetCampaignOrderedItemsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.SchoolID == null)
            return Result<IReadOnlyList<CampaignOrderedItemDto>>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var campaign = await _db.Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.SchoolID == user.SchoolID.Value, ct);
        if (campaign == null)
            return Result<IReadOnlyList<CampaignOrderedItemDto>>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        // Get all order items for this campaign
        var orderItems = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.ProductVariant).ThenInclude(pv => pv.Outfit)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.CampaignID == query.CampaignId)
            .ToListAsync(ct);

        var grouped = orderItems
            .GroupBy(oi => new { oi.ProductVariant.OutfitID, oi.ProductVariant.Outfit.OutfitName, oi.ProductVariant.Outfit.MainImageURL })
            .Select(g => new CampaignOrderedItemDto(
                g.Key.OutfitID,
                g.Key.OutfitName,
                g.Key.MainImageURL,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.Quantity * x.UnitPrice),
                g.GroupBy(x => new { x.SizeOrdered, x.ProductVariant.ColorVariant })
                 .Select(sg => new OrderedVariantDto(sg.Key.SizeOrdered, sg.Key.ColorVariant, sg.Sum(x => x.Quantity)))
                 .ToList()
            ))
            .ToList();

        return Result<IReadOnlyList<CampaignOrderedItemDto>>.Success(grouped);
    }
}
