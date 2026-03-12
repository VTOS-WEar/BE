using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;

namespace VTOS.Application.Features.Public.Queries;

public class GetPublicCampaignDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetPublicCampaignDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublicCampaignDetailResponse?> HandleAsync(
        GetPublicCampaignDetailQuery query,
        CancellationToken ct = default)
    {
        var campaign = await _context.Campaigns
            .AsNoTracking()
            .Where(c => c.Id == query.CampaignId)
            .Select(c => new PublicCampaignDetailResponse(
                c.Id,
                c.CampaignName,
                c.Status.ToString(),
                c.StartDate,
                c.EndDate,
                c.Description,
                new PublicSchoolSummaryDto(
                    c.School.Id,
                    c.School.SchoolName,
                    c.School.LogoURL
                ),
                c.CampaignOutfits
                    .Select(co => new PublicCampaignOutfitDto(
                        co.Id,
                        co.OutfitID,
                        co.Outfit.OutfitName,
                        co.Outfit.MainImageURL,
                        co.CampaignPrice,
                        co.MaxQuantity
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(ct);

        return campaign;
    }
}
