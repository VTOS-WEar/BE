using Microsoft.EntityFrameworkCore;
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

    public GetCampaignDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<CampaignDetailDto>> HandleAsync(GetCampaignDetailQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.SchoolID == null)
            return Result<CampaignDetailDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var campaign = await _db.Campaigns
            .AsNoTracking()
            .Include(c => c.CampaignOutfits)
                .ThenInclude(co => co.Outfit)
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.SchoolID == user.SchoolID.Value, ct);

        if (campaign == null)
            return Result<CampaignDetailDto>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        var outfitDtos = campaign.CampaignOutfits.Select(co => new CampaignOutfitDetailDto(
            co.Id, co.OutfitID, co.Outfit.OutfitName, co.Outfit.MainImageURL,
            co.CampaignPrice, co.MaxQuantity, co.ProviderID
        )).ToList();

        var dto = new CampaignDetailDto(
            campaign.Id, campaign.CampaignName, campaign.Status.ToString(),
            campaign.StartDate, campaign.EndDate, campaign.Description,
            campaign.CreatedAt, campaign.Orders.Count, outfitDtos
        );

        return Result<CampaignDetailDto>.Success(dto);
    }
}
