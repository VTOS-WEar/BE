using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetCampaignDetailQuery(Guid UserId, Guid CampaignId);

public record CampaignOutfitDetailDto(
    Guid CampaignOutfitId,
    Guid OutfitId,
    string OutfitName,
    string? MainImageUrl,
    decimal CampaignPrice,
    int? MaxQuantity,
    Guid? ProviderId
);

public record CampaignDetailDto(
    Guid CampaignId,
    string CampaignName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    DateTime CreatedAt,
    int TotalOrders,
    IReadOnlyList<CampaignOutfitDetailDto> Outfits
);

public interface IGetCampaignDetailQueryHandler
{
    Task<Result<CampaignDetailDto>> HandleAsync(GetCampaignDetailQuery query, CancellationToken ct = default);
}

public class GetCampaignDetailQueryHandler : IGetCampaignDetailQueryHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<GetCampaignDetailQueryHandler> _logger;

    public GetCampaignDetailQueryHandler(IApplicationDbContext db, ILogger<GetCampaignDetailQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<CampaignDetailDto>> HandleAsync(GetCampaignDetailQuery query, CancellationToken ct = default)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
            if (user?.SchoolID == null)
                return Result<CampaignDetailDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

            var campaign = await _db.Campaigns
                .AsNoTracking()
                .Include(c => c.CampaignOutfits)
                    .ThenInclude(co => co.Outfit)
                .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.SchoolID == user.SchoolID.Value, ct);

            if (campaign == null)
                return Result<CampaignDetailDto>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

            // Count orders separately to avoid loading full Order entity
            // (which may have columns not yet in DB like CancelReason)
            var orderCount = await _db.Orders
                .AsNoTracking()
                .CountAsync(o => o.CampaignID == query.CampaignId, ct);

            var outfitDtos = campaign.CampaignOutfits
                .Where(co => co.Outfit != null) // protect against deleted outfits
                .Select(co => new CampaignOutfitDetailDto(
                    co.Id, co.OutfitID, co.Outfit.OutfitName, co.Outfit.MainImageURL,
                    co.CampaignPrice, co.MaxQuantity, co.ProviderID
                )).ToList();

            var dto = new CampaignDetailDto(
                campaign.Id, campaign.CampaignName, campaign.Status.ToString(),
                campaign.StartDate, campaign.EndDate, campaign.Description,
                campaign.CreatedAt, orderCount, outfitDtos
            );

            return Result<CampaignDetailDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting campaign detail for CampaignId={CampaignId}, UserId={UserId}",
                query.CampaignId, query.UserId);
            return Result<CampaignDetailDto>.Failure($"Internal error: {ex.Message}", "INTERNAL_ERROR");
        }
    }
}
