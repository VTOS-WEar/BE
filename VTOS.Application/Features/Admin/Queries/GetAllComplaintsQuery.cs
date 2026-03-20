using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

// ── DTOs ──

public record AdminComplaintDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string SchoolName { get; init; } = string.Empty;
    public string? ProviderName { get; init; }
    public string? CampaignName { get; init; }
    public string? BatchName { get; init; }
    public string? Response { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public record AdminComplaintListResult
{
    public List<AdminComplaintDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int OpenCount { get; init; }
    public int InProgressCount { get; init; }
    public int ResolvedCount { get; init; }
}

// ── Interface ──

public interface IGetAllComplaintsQueryHandler
{
    Task<Result<AdminComplaintListResult>> HandleAsync(
        int page = 1, int pageSize = 20,
        ComplaintStatus? status = null,
        CancellationToken ct = default);
}

// ── Handler ──

public class GetAllComplaintsQueryHandler : IGetAllComplaintsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAllComplaintsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<AdminComplaintListResult>> HandleAsync(
        int page = 1, int pageSize = 20,
        ComplaintStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _context.Complaints
            .Include(c => c.School)
            .Include(c => c.Provider)
            .Include(c => c.Campaign)
            .Include(c => c.Batch)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        // Status counts (unfiltered)
        var allComplaints = _context.Complaints.AsQueryable();
        var openCount = await allComplaints.CountAsync(c => c.Status == ComplaintStatus.Open, ct);
        var inProgressCount = await allComplaints.CountAsync(c => c.Status == ComplaintStatus.InProgress, ct);
        var resolvedCount = await allComplaints.CountAsync(c => c.Status == ComplaintStatus.Resolved, ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminComplaintDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Status = c.Status.ToString(),
                SchoolName = c.School.SchoolName,
                ProviderName = c.Provider != null ? c.Provider.ProviderName : null,
                CampaignName = c.Campaign.CampaignName,
                BatchName = c.Batch != null ? c.Batch.Id.ToString().Substring(0, 8).ToUpper() : null,
                Response = c.Response,
                CreatedAt = c.CreatedAt,
                RespondedAt = c.RespondedAt,
                ResolvedAt = c.ResolvedAt
            })
            .ToListAsync(ct);

        return Result<AdminComplaintListResult>.Success(new AdminComplaintListResult
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
