using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

public record GetProductionSupportTicketsQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record SupportTicketDto(
    Guid ComplaintId,
    Guid? OrderId,
    Guid? SemesterPublicationId,
    string? SemesterLabel,
    Guid? ProviderId,
    string? ProviderName,
    string Title,
    string Description,
    string? Response,
    List<string>? ProofImageUrls,
    string Status,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    DateTime? ResolvedAt
);

public record GetProductionSupportTicketsResponse(
    IReadOnlyList<SupportTicketDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetProductionSupportTicketsQueryHandler
{
    Task<Result<GetProductionSupportTicketsResponse>> HandleAsync(GetProductionSupportTicketsQuery query, CancellationToken ct = default);
}

public class GetProductionSupportTicketsQueryHandler : IGetProductionSupportTicketsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProductionSupportTicketsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetProductionSupportTicketsResponse>> HandleAsync(GetProductionSupportTicketsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null)
            return Result<GetProductionSupportTicketsResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<GetProductionSupportTicketsResponse>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolMgr.SchoolID;

        var q = _db.SupportTickets.AsNoTracking()
            .Include(c => c.Order)
            .Include(c => c.SemesterPublication)
            .Include(c => c.Provider)
            .Where(c => c.SchoolID == schoolId);

        // Status filter
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<SupportTicketStatus>(query.Status, true, out var statusEnum))
            q = q.Where(c => c.Status == statusEnum);

        q = q.OrderByDescending(c => c.CreatedAt);

        var total = await q.CountAsync(ct);
        var rawItems = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new
            {
                c.Id, c.OrderID, c.SemesterPublicationID,
                SemesterLabel = c.SemesterPublication != null ? $"{c.SemesterPublication.Semester} {c.SemesterPublication.AcademicYear}" : null,
                c.ProviderID, ProviderName = c.Provider != null ? c.Provider.ProviderName : null,
                c.Title, c.Description, c.Response, c.ProofImageUrls,
                Status = c.Status.ToString(), c.CreatedAt, c.RespondedAt, c.ResolvedAt
            })
            .ToListAsync(ct);

        var items = rawItems.Select(c => new SupportTicketDto(
            c.Id, c.OrderID, c.SemesterPublicationID, c.SemesterLabel,
            c.ProviderID, c.ProviderName,
            c.Title, c.Description, c.Response,
            string.IsNullOrEmpty(c.ProofImageUrls) ? null : JsonSerializer.Deserialize<List<string>>(c.ProofImageUrls),
            c.Status, c.CreatedAt, c.RespondedAt, c.ResolvedAt
        )).ToList();

        return Result<GetProductionSupportTicketsResponse>.Success(
            new GetProductionSupportTicketsResponse(items, total, query.Page, query.PageSize));
    }
}

