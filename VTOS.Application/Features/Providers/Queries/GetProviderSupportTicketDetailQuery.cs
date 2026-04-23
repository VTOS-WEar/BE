using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderSupportTicketDetailQuery(Guid UserId, Guid ComplaintId);

public interface IGetProviderSupportTicketDetailQueryHandler
{
    Task<Result<SupportTicketDetailDto>> HandleAsync(GetProviderSupportTicketDetailQuery query, CancellationToken ct = default);
}

public class GetProviderSupportTicketDetailQueryHandler : IGetProviderSupportTicketDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderSupportTicketDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SupportTicketDetailDto>> HandleAsync(GetProviderSupportTicketDetailQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null)
            return Result<SupportTicketDetailDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<SupportTicketDetailDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var c = await _db.SupportTickets.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.SemesterPublication)
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x => x.Id == query.ComplaintId && x.ProviderID == providerMgr.ProviderID, ct);

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
