using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetComplaintDetailQuery(Guid UserId, Guid ComplaintId);

public record ComplaintDetailDto(
    Guid ComplaintId,
    Guid CampaignId,
    string? CampaignName,
    Guid? BatchId,
    string? BatchName,
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

public interface IGetComplaintDetailQueryHandler
{
    Task<Result<ComplaintDetailDto>> HandleAsync(GetComplaintDetailQuery query, CancellationToken ct = default);
}

public class GetComplaintDetailQueryHandler : IGetComplaintDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetComplaintDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ComplaintDetailDto>> HandleAsync(GetComplaintDetailQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<ComplaintDetailDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var c = await _db.Complaints.AsNoTracking()
            .Include(x => x.Campaign)
            .Include(x => x.Batch)
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x => x.Id == query.ComplaintId && x.SchoolID == schoolMgr.SchoolID, ct);

        if (c == null)
            return Result<ComplaintDetailDto>.Failure("Complaint not found.", "COMPLAINT_NOT_FOUND");

        return Result<ComplaintDetailDto>.Success(new ComplaintDetailDto(
            c.Id, c.CampaignID, c.Campaign?.CampaignName,
            c.BatchID, c.Batch?.BatchName,
            c.ProviderID, c.Provider?.ProviderName,
            c.Title, c.Description, c.Response,
            c.Status.ToString(), c.CreatedAt,
            c.RespondedAt, c.ResolvedAt
        ));
    }
}
