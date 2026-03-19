using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

public record GetProductionComplaintsQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record ComplaintDto(
    Guid ComplaintId,
    Guid CampaignId,
    string? CampaignName,
    Guid? BatchId,
    Guid? ProviderId,
    string? ProviderName,
    string Title,
    string Description,
    string? Response,
    string Status,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    DateTime? ResolvedAt
);

public record GetProductionComplaintsResponse(
    IReadOnlyList<ComplaintDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetProductionComplaintsQueryHandler
{
    Task<Result<GetProductionComplaintsResponse>> HandleAsync(GetProductionComplaintsQuery query, CancellationToken ct = default);
}

public class GetProductionComplaintsQueryHandler : IGetProductionComplaintsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProductionComplaintsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetProductionComplaintsResponse>> HandleAsync(GetProductionComplaintsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<GetProductionComplaintsResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolMgr.SchoolID;

        var q = _db.Complaints.AsNoTracking()
            .Include(c => c.Campaign)
            .Include(c => c.Provider)
            .Where(c => c.SchoolID == schoolId);

        // Status filter
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

        return Result<GetProductionComplaintsResponse>.Success(
            new GetProductionComplaintsResponse(items, total, query.Page, query.PageSize));
    }
}

