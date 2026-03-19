using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Queries;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderComplaintsQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record GetProviderComplaintsResponse(
    IReadOnlyList<ComplaintDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetProviderComplaintsQueryHandler
{
    Task<Result<GetProviderComplaintsResponse>> HandleAsync(GetProviderComplaintsQuery query, CancellationToken ct = default);
}

public class GetProviderComplaintsQueryHandler : IGetProviderComplaintsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderComplaintsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetProviderComplaintsResponse>> HandleAsync(GetProviderComplaintsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<GetProviderComplaintsResponse>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var providerId = providerMgr.ProviderID;

        var q = _db.Complaints.AsNoTracking()
            .Include(c => c.Campaign)
            .Include(c => c.Provider)
            .Where(c => c.ProviderID == providerId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ComplaintStatus>(query.Status, true, out var statusEnum))
            q = q.Where(c => c.Status == statusEnum);

        q = q.OrderByDescending(c => c.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new ComplaintDto(
                c.Id, c.CampaignID, c.Campaign.CampaignName,
                c.BatchID, c.ProviderID, c.Provider != null ? c.Provider.ProviderName : null,
                c.Title, c.Description, c.Response,
                c.Status.ToString(), c.CreatedAt, c.RespondedAt, c.ResolvedAt
            ))
            .ToListAsync(ct);

        return Result<GetProviderComplaintsResponse>.Success(
            new GetProviderComplaintsResponse(items, total, query.Page, query.PageSize));
    }
}
