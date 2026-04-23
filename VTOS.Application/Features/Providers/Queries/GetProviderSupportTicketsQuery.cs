using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Queries;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderSupportTicketsQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record GetProviderSupportTicketsResponse(
    IReadOnlyList<SupportTicketDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetProviderSupportTicketsQueryHandler
{
    Task<Result<GetProviderSupportTicketsResponse>> HandleAsync(GetProviderSupportTicketsQuery query, CancellationToken ct = default);
}

public class GetProviderSupportTicketsQueryHandler : IGetProviderSupportTicketsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderSupportTicketsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetProviderSupportTicketsResponse>> HandleAsync(GetProviderSupportTicketsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null)
            return Result<GetProviderSupportTicketsResponse>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<GetProviderSupportTicketsResponse>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var providerId = providerMgr.ProviderID;

        var q = _db.SupportTickets.AsNoTracking()
            .Include(c => c.Order)
            .Include(c => c.SemesterPublication)
            .Include(c => c.Provider)
            .Where(c => c.ProviderID == providerId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<SupportTicketStatus>(query.Status, true, out var statusEnum))
            q = q.Where(c => c.Status == statusEnum);

        q = q.OrderByDescending(c => c.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new SupportTicketDto(
                c.Id,
                c.OrderID,
                c.SemesterPublicationID,
                c.SemesterPublication != null ? $"{c.SemesterPublication.Semester} {c.SemesterPublication.AcademicYear}" : null,
                c.ProviderID, c.Provider != null ? c.Provider.ProviderName : null,
                c.Title, c.Description, c.Response,
                c.Status.ToString(), c.CreatedAt, c.RespondedAt, c.ResolvedAt
            ))
            .ToListAsync(ct);

        return Result<GetProviderSupportTicketsResponse>.Success(
            new GetProviderSupportTicketsResponse(items, total, query.Page, query.PageSize));
    }
}
