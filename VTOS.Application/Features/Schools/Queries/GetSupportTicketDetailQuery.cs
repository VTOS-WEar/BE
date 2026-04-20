using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetSupportTicketDetailQuery(Guid UserId, Guid ComplaintId);

public record SupportTicketDetailDto(
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

public interface IGetSupportTicketDetailQueryHandler
{
    Task<Result<SupportTicketDetailDto>> HandleAsync(GetSupportTicketDetailQuery query, CancellationToken ct = default);
}

public class GetSupportTicketDetailQueryHandler : IGetSupportTicketDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSupportTicketDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SupportTicketDetailDto>> HandleAsync(GetSupportTicketDetailQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<SupportTicketDetailDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var c = await _db.SupportTickets.AsNoTracking()
            .Include(x => x.Campaign)
            .Include(x => x.Batch)
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x => x.Id == query.ComplaintId && x.SchoolID == schoolMgr.SchoolID, ct);

        if (c == null)
            return Result<SupportTicketDetailDto>.Failure("SupportTicket not found.", "COMPLAINT_NOT_FOUND");

        return Result<SupportTicketDetailDto>.Success(new SupportTicketDetailDto(
            c.Id, c.CampaignID, c.Campaign?.CampaignName,
            c.BatchID, c.Batch?.BatchName,
            c.ProviderID, c.Provider?.ProviderName,
            c.Title, c.Description, c.Response,
            c.Status.ToString(), c.CreatedAt,
            c.RespondedAt, c.ResolvedAt
        ));
    }
}
