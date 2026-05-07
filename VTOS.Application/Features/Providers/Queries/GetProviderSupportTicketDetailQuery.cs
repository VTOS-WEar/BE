using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderSupportTicketDetailQuery(Guid UserId, Guid ComplaintId);

public interface IGetProviderSupportTicketDetailQueryHandler
{
    Task<Result<ProviderSupportTicketDto>> HandleAsync(GetProviderSupportTicketDetailQuery query, CancellationToken ct = default);
}

public class GetProviderSupportTicketDetailQueryHandler : IGetProviderSupportTicketDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderSupportTicketDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ProviderSupportTicketDto>> HandleAsync(GetProviderSupportTicketDetailQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null)
            return Result<ProviderSupportTicketDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<ProviderSupportTicketDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var c = await _db.SupportTickets.AsNoTracking()
            .Include(x => x.SemesterPublication)
            .Include(x => x.Provider)
            .Where(x => x.Id == query.ComplaintId
                && x.ProviderID == providerMgr.ProviderID
                && x.RequesterRole != "Provider")
            .Select(x => new
            {
                x.Id,
                x.OrderID,
                x.SemesterPublicationID,
                SemesterLabel = x.SemesterPublication != null ? $"{x.SemesterPublication.Semester} {x.SemesterPublication.AcademicYear}" : null,
                x.ProviderID,
                ProviderName = x.Provider != null ? x.Provider.ProviderName : null,
                x.Title,
                x.Description,
                x.Response,
                x.ProofImageUrls,
                Status = x.Status.ToString(),
                x.Category,
                x.RequesterRole,
                x.CreatedAt,
                x.RespondedAt,
                x.ResolvedAt
            })
            .FirstOrDefaultAsync(ct);

        if (c == null)
            return Result<ProviderSupportTicketDto>.Failure("SupportTicket not found.", "COMPLAINT_NOT_FOUND");

        return Result<ProviderSupportTicketDto>.Success(new ProviderSupportTicketDto(
            c.Id,
            c.OrderID,
            c.SemesterPublicationID,
            c.SemesterLabel,
            c.ProviderID,
            c.ProviderName,
            c.Title,
            c.Description,
            c.Response,
            string.IsNullOrEmpty(c.ProofImageUrls) ? null : JsonSerializer.Deserialize<List<string>>(c.ProofImageUrls),
            c.Status,
            c.Category,
            c.RequesterRole,
            c.CreatedAt,
            c.RespondedAt,
            c.ResolvedAt));
    }
}
