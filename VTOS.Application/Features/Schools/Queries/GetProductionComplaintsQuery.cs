using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetProductionComplaintsQuery(Guid UserId, int Page = 1, int PageSize = 10);

public record ComplaintDto(
    Guid ComplaintId,
    Guid CampaignId,
    string? CampaignName,
    Guid? BatchId,
    string Title,
    string Description,
    string Status,
    DateTime CreatedAt,
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
        if (user?.SchoolID == null)
            return Result<GetProductionComplaintsResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var schoolId = user.SchoolID.Value;

        var q = _db.Complaints.AsNoTracking()
            .Include(c => c.Campaign)
            .Where(c => c.SchoolID == schoolId)
            .OrderByDescending(c => c.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new ComplaintDto(
                c.Id, c.CampaignID, c.Campaign.CampaignName,
                c.BatchID, c.Title, c.Description,
                c.Status.ToString(), c.CreatedAt, c.ResolvedAt
            ))
            .ToListAsync(ct);

        return Result<GetProductionComplaintsResponse>.Success(
            new GetProductionComplaintsResponse(items, total, query.Page, query.PageSize));
    }
}
