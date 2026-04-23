using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetSupportTicketDetailQuery(Guid UserId, Guid ComplaintId);

public record SupportTicketDetailDto(
    Guid ComplaintId,
    Guid? OrderId,
    Guid? SemesterPublicationId,
    string? SemesterLabel,
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
        if (user == null)
            return Result<SupportTicketDetailDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<SupportTicketDetailDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var c = await _db.SupportTickets.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.SemesterPublication)
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x => x.Id == query.ComplaintId && x.SchoolID == schoolMgr.SchoolID, ct);

        if (c == null)
            return Result<SupportTicketDetailDto>.Failure("SupportTicket not found.", "COMPLAINT_NOT_FOUND");

        return Result<SupportTicketDetailDto>.Success(new SupportTicketDetailDto(
            c.Id,
            c.OrderID,
            c.SemesterPublicationID,
            c.SemesterPublication != null ? $"{c.SemesterPublication.Semester} {c.SemesterPublication.AcademicYear}" : null,
            c.ProviderID, c.Provider?.ProviderName,
            c.Title, c.Description, c.Response,
            c.Status.ToString(), c.CreatedAt,
            c.RespondedAt, c.ResolvedAt
        ));
    }
}
