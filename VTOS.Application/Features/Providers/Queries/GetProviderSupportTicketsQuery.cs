using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderSupportTicketsQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record GetProviderSupportTicketsResponse(
    IReadOnlyList<ProviderSupportTicketDto> Items,
    int Total,
    int Page,
    int PageSize
);

public record ProviderSupportTicketDto(
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
    string Category,
    string RequesterRole,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    DateTime? ResolvedAt
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
            .Where(c => c.ProviderID == providerId
                && c.RequesterRole != "Provider");

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
                c.Category, c.RequesterRole,
                Status = c.Status.ToString(), c.CreatedAt, c.RespondedAt, c.ResolvedAt
            })
            .ToListAsync(ct);

        var items = rawItems.Select(c => new ProviderSupportTicketDto(
            c.Id, c.OrderID, c.SemesterPublicationID, c.SemesterLabel,
            c.ProviderID, c.ProviderName,
            c.Title, c.Description, c.Response,
            string.IsNullOrEmpty(c.ProofImageUrls) ? null : JsonSerializer.Deserialize<List<string>>(c.ProofImageUrls),
            c.Status, c.Category, c.RequesterRole, c.CreatedAt, c.RespondedAt, c.ResolvedAt
        )).ToList();

        return Result<GetProviderSupportTicketsResponse>.Success(
            new GetProviderSupportTicketsResponse(items, total, query.Page, query.PageSize));
    }
}
