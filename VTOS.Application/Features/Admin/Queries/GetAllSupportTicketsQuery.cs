using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

// ── DTOs ──

public record AdminSupportTicketDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RequesterRole { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public string RequesterEmail { get; init; } = string.Empty;
    public string? SchoolName { get; init; }
    public string? ProviderName { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? SemesterPublicationId { get; init; }
    public string? SemesterLabel { get; init; }
    public string? Response { get; init; }
    public List<string>? ProofImageUrls { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public record AdminSupportTicketListResult
{
    public List<AdminSupportTicketDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int OpenCount { get; init; }
    public int InProgressCount { get; init; }
    public int ResolvedCount { get; init; }
}

// ── Interface ──

public interface IGetAllSupportTicketsQueryHandler
{
    Task<Result<AdminSupportTicketListResult>> HandleAsync(
        int page = 1, int pageSize = 20,
        SupportTicketStatus? status = null,
        string? search = null,
        string? requesterRole = null,
        string? category = null,
        CancellationToken ct = default);
}

// ── Handler ──

public class GetAllSupportTicketsQueryHandler : IGetAllSupportTicketsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAllSupportTicketsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<AdminSupportTicketListResult>> HandleAsync(
        int page = 1, int pageSize = 20,
        SupportTicketStatus? status = null,
        string? search = null,
        string? requesterRole = null,
        string? category = null,
        CancellationToken ct = default)
    {
        var query = _context.SupportTickets
            .Include(c => c.School)
            .Include(c => c.Provider)
            .Include(c => c.Order)
            .Include(c => c.SemesterPublication)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(s) ||
                c.RequesterName.ToLower().Contains(s) ||
                c.RequesterEmail.ToLower().Contains(s) ||
                (c.School != null && c.School.SchoolName.ToLower().Contains(s)));
        }
        if (!string.IsNullOrWhiteSpace(requesterRole))
            query = query.Where(c => c.RequesterRole == requesterRole);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        var totalCount = await query.CountAsync(ct);

        // Status counts (unfiltered)
        var allComplaints = _context.SupportTickets.AsQueryable();
        var openCount = await allComplaints.CountAsync(c => c.Status == SupportTicketStatus.Open, ct);
        var inProgressCount = await allComplaints.CountAsync(c => c.Status == SupportTicketStatus.InProgress, ct);
        var resolvedCount = await allComplaints.CountAsync(c => c.Status == SupportTicketStatus.Resolved, ct);

        var rawItems = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id, c.Title, c.Description, c.Category,
                Status = c.Status.ToString(),
                c.RequesterRole, c.RequesterName, c.RequesterEmail,
                SchoolName = c.School != null ? c.School.SchoolName : null,
                ProviderName = c.Provider != null ? c.Provider.ProviderName : null,
                OrderId = c.OrderID,
                SemesterPublicationId = c.SemesterPublicationID,
                SemesterLabel = c.SemesterPublication != null ? $"{c.SemesterPublication.Semester} {c.SemesterPublication.AcademicYear}" : null,
                c.Response, c.ProofImageUrls,
                c.CreatedAt, c.RespondedAt, c.ResolvedAt
            })
            .ToListAsync(ct);

        var items = rawItems.Select(c => new AdminSupportTicketDto
        {
            Id = c.Id, Title = c.Title, Description = c.Description,
            Category = c.Category, Status = c.Status,
            RequesterRole = c.RequesterRole, RequesterName = c.RequesterName,
            RequesterEmail = c.RequesterEmail,
            SchoolName = c.SchoolName, ProviderName = c.ProviderName,
            OrderId = c.OrderId, SemesterPublicationId = c.SemesterPublicationId,
            SemesterLabel = c.SemesterLabel, Response = c.Response,
            ProofImageUrls = string.IsNullOrEmpty(c.ProofImageUrls) ? null : JsonSerializer.Deserialize<List<string>>(c.ProofImageUrls),
            CreatedAt = c.CreatedAt, RespondedAt = c.RespondedAt, ResolvedAt = c.ResolvedAt
        }).ToList();

        return Result<AdminSupportTicketListResult>.Success(new AdminSupportTicketListResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            OpenCount = openCount,
            InProgressCount = inProgressCount,
            ResolvedCount = resolvedCount
        });
    }
}
