using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetCampaignListQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record CampaignListItemDto(
    Guid CampaignId,
    string CampaignName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    int OutfitCount,
    int OrderCount
);

public record GetCampaignListResponse(
    IReadOnlyList<CampaignListItemDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetCampaignListQueryHandler
{
    Task<Result<GetCampaignListResponse>> HandleAsync(GetCampaignListQuery query, CancellationToken ct = default);
}

public class GetCampaignListQueryHandler : IGetCampaignListQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetCampaignListQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetCampaignListResponse>> HandleAsync(GetCampaignListQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.SchoolID == null)
            return Result<GetCampaignListResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var schoolId = user.SchoolID.Value;

        var q = _db.Campaigns.AsNoTracking()
            .Where(c => c.SchoolID == schoolId);

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<Domain.Enums.CampaignStatus>(query.Status, true, out var statusEnum))
            q = q.Where(c => c.Status == statusEnum);

        var total = await q.CountAsync(ct);

        var campaigns = await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CampaignListItemDto(
                c.Id,
                c.CampaignName,
                c.Status.ToString(),
                c.StartDate,
                c.EndDate,
                c.Description,
                c.CampaignOutfits.Count,
                c.Orders.Count
            ))
            .ToListAsync(ct);

        return Result<GetCampaignListResponse>.Success(new GetCampaignListResponse(campaigns, total, query.Page, query.PageSize));
    }
}
