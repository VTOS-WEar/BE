using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.SupportTickets;

public record CreateSupportTicketCommand(Guid UserId, CreateSupportTicketRequestDto Request);
public record GetMySupportTicketsQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);
public record GetMySupportTicketDetailQuery(Guid UserId, Guid TicketId);

public interface ICreateSupportTicketCommandHandler
{
    Task<Result<SupportTicketResponseDto>> HandleAsync(CreateSupportTicketCommand command, CancellationToken ct = default);
}

public interface IGetMySupportTicketsQueryHandler
{
    Task<Result<SupportTicketListResult>> HandleAsync(GetMySupportTicketsQuery query, CancellationToken ct = default);
}

public interface IGetMySupportTicketDetailQueryHandler
{
    Task<Result<SupportTicketResponseDto>> HandleAsync(GetMySupportTicketDetailQuery query, CancellationToken ct = default);
}

public class CreateSupportTicketCommandHandler : ICreateSupportTicketCommandHandler
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Parent",
        "Provider",
        "School",
        "HomeroomTeacher"
    };

    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public CreateSupportTicketCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<SupportTicketResponseDto>> HandleAsync(CreateSupportTicketCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Title))
            return Result<SupportTicketResponseDto>.Failure("Title is required.", "TITLE_REQUIRED");

        if (string.IsNullOrWhiteSpace(command.Request.Description))
            return Result<SupportTicketResponseDto>.Failure("Description is required.", "DESCRIPTION_REQUIRED");

        var context = await ResolveRequesterContextAsync(command.UserId, ct);
        if (context == null)
            return Result<SupportTicketResponseDto>.Failure("User not found.", "USER_NOT_FOUND");

        if (!AllowedRoles.Contains(context.Role))
            return Result<SupportTicketResponseDto>.Failure("This role cannot create support tickets.", "ROLE_NOT_ALLOWED");

        var now = DateTime.UtcNow;
        // Resolve ProviderID from Order when requester is not a Provider
        var resolvedProviderId = context.ProviderId;
        if (command.Request.OrderId.HasValue && resolvedProviderId == null)
        {
            resolvedProviderId = await _db.Orders.AsNoTracking()
                .Where(o => o.Id == command.Request.OrderId.Value)
                .Select(o => o.ProviderID)
                .FirstOrDefaultAsync(ct);
        }

        // Serialize proof image URLs
        string? proofJson = null;
        if (command.Request.ProofImageUrls is { Count: > 0 })
        {
            proofJson = JsonSerializer.Serialize(command.Request.ProofImageUrls);
        }

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            RequesterUserID = context.UserId,
            RequesterRole = context.Role,
            RequesterName = context.Name,
            RequesterEmail = context.Email,
            SchoolID = context.SchoolId,
            ProviderID = resolvedProviderId,
            OrderID = command.Request.OrderId,
            SemesterPublicationID = command.Request.SemesterPublicationId,
            Category = NormalizeCategory(command.Request.Category),
            Title = command.Request.Title.Trim(),
            Description = command.Request.Description.Trim(),
            ProofImageUrls = proofJson,
            Status = SupportTicketStatus.Open,
            CreatedAt = now,
            CreatedBy = context.Email
        };

        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _notificationService.NotifyAdminsAsync(
                "Yeu cau ho tro moi",
                $"{context.Role} {context.Name} da gui yeu cau ho tro: {ticket.Title}",
                "SupportTicket",
                ticket.Id,
                "SupportTicket",
                "/admin/complaints",
                ct);
        }
        catch
        {
            // Ticket creation should not fail because notification delivery failed.
        }

        var dto = await ProjectTicketById(ticket.Id, _db.SupportTickets.AsNoTracking(), ct);
        return dto == null
            ? Result<SupportTicketResponseDto>.Failure("Support ticket was created but could not be loaded.", "LOAD_FAILED")
            : Result<SupportTicketResponseDto>.Success(dto);
    }

    private async Task<RequesterContext?> ResolveRequesterContextAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);

        if (user == null)
            return null;

        Guid? schoolId = null;
        Guid? providerId = null;

        if (string.Equals(user.Role.RoleName, "School", StringComparison.OrdinalIgnoreCase))
        {
            schoolId = await _db.SchoolManagers
                .AsNoTracking()
                .Where(m => m.UserID == user.Id)
                .Select(m => (Guid?)m.SchoolID)
                .FirstOrDefaultAsync(ct);
        }
        else if (string.Equals(user.Role.RoleName, "Provider", StringComparison.OrdinalIgnoreCase))
        {
            providerId = await _db.ProviderManagers
                .AsNoTracking()
                .Where(m => m.UserID == user.Id)
                .Select(m => (Guid?)m.ProviderID)
                .FirstOrDefaultAsync(ct);
        }
        else if (string.Equals(user.Role.RoleName, "HomeroomTeacher", StringComparison.OrdinalIgnoreCase))
        {
            schoolId = await _db.ClassGroups
                .AsNoTracking()
                .Where(c => c.HomeroomTeacherID == user.Id)
                .OrderBy(c => c.ClassName)
                .Select(c => (Guid?)c.SchoolID)
                .FirstOrDefaultAsync(ct);
        }

        return new RequesterContext(
            user.Id,
            user.Role.RoleName,
            string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName,
            user.Email,
            schoolId,
            providerId);
    }

    private static string NormalizeCategory(string? category)
    {
        var value = category?.Trim();
        return string.IsNullOrWhiteSpace(value) ? "General" : value[..Math.Min(value.Length, 80)];
    }

    private sealed record RequesterContext(Guid UserId, string Role, string Name, string Email, Guid? SchoolId, Guid? ProviderId);

    internal static async Task<SupportTicketResponseDto?> ProjectTicketById(
        Guid ticketId,
        IQueryable<SupportTicket> source,
        CancellationToken ct)
    {
        var raw = await source
            .Where(t => t.Id == ticketId)
            .Include(t => t.School)
            .Include(t => t.Provider)
            .Include(t => t.SemesterPublication)
            .Select(t => new
            {
                t.Id, t.Title, t.Description, t.Category,
                Status = t.Status.ToString(),
                t.RequesterRole, t.RequesterName, t.RequesterEmail,
                SchoolName = t.School != null ? t.School.SchoolName : null,
                ProviderName = t.Provider != null ? t.Provider.ProviderName : null,
                t.OrderID, t.SemesterPublicationID,
                SemesterLabel = t.SemesterPublication != null ? $"{t.SemesterPublication.Semester} {t.SemesterPublication.AcademicYear}" : null,
                t.Response, t.ProofImageUrls,
                t.CreatedAt, t.RespondedAt, t.ResolvedAt
            })
            .FirstOrDefaultAsync(ct);

        if (raw == null) return null;

        return new SupportTicketResponseDto(
            raw.Id, raw.Title, raw.Description, raw.Category, raw.Status,
            raw.RequesterRole, raw.RequesterName, raw.RequesterEmail,
            raw.SchoolName, raw.ProviderName,
            raw.OrderID, raw.SemesterPublicationID, raw.SemesterLabel,
            raw.Response,
            ParseProofUrls(raw.ProofImageUrls),
            raw.CreatedAt, raw.RespondedAt, raw.ResolvedAt);
    }

    internal static List<string>? ParseProofUrls(string? json)
        => string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<List<string>>(json);
}

public class GetMySupportTicketsQueryHandler : IGetMySupportTicketsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetMySupportTicketsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SupportTicketListResult>> HandleAsync(GetMySupportTicketsQuery query, CancellationToken ct = default)
    {
        var tickets = _db.SupportTickets
            .AsNoTracking()
            .Where(t => t.RequesterUserID == query.UserId);

        var countsQuery = tickets;
        var openCount = await countsQuery.CountAsync(t => t.Status == SupportTicketStatus.Open, ct);
        var inProgressCount = await countsQuery.CountAsync(t => t.Status == SupportTicketStatus.InProgress, ct);
        var resolvedCount = await countsQuery.CountAsync(t => t.Status == SupportTicketStatus.Resolved, ct);
        var closedCount = await countsQuery.CountAsync(t => t.Status == SupportTicketStatus.Closed, ct);

        if (SupportTicketStatusParser.TryParse(query.Status, out var status))
            tickets = tickets.Where(t => t.Status == status);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var total = await tickets.CountAsync(ct);

        var rawItems = await tickets
            .Include(t => t.School)
            .Include(t => t.Provider)
            .Include(t => t.SemesterPublication)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id, t.Title, t.Description, t.Category,
                Status = t.Status.ToString(),
                t.RequesterRole, t.RequesterName, t.RequesterEmail,
                SchoolName = t.School != null ? t.School.SchoolName : null,
                ProviderName = t.Provider != null ? t.Provider.ProviderName : null,
                t.OrderID, t.SemesterPublicationID,
                SemesterLabel = t.SemesterPublication != null ? $"{t.SemesterPublication.Semester} {t.SemesterPublication.AcademicYear}" : null,
                t.Response, t.ProofImageUrls,
                t.CreatedAt, t.RespondedAt, t.ResolvedAt
            })
            .ToListAsync(ct);

        var items = rawItems.Select(r => new SupportTicketResponseDto(
            r.Id, r.Title, r.Description, r.Category, r.Status,
            r.RequesterRole, r.RequesterName, r.RequesterEmail,
            r.SchoolName, r.ProviderName,
            r.OrderID, r.SemesterPublicationID, r.SemesterLabel,
            r.Response,
            CreateSupportTicketCommandHandler.ParseProofUrls(r.ProofImageUrls),
            r.CreatedAt, r.RespondedAt, r.ResolvedAt)).ToList();

        return Result<SupportTicketListResult>.Success(new SupportTicketListResult(
            items,
            total,
            page,
            pageSize,
            openCount,
            inProgressCount,
            resolvedCount,
            closedCount));
    }
}

public class GetMySupportTicketDetailQueryHandler : IGetMySupportTicketDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetMySupportTicketDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SupportTicketResponseDto>> HandleAsync(GetMySupportTicketDetailQuery query, CancellationToken ct = default)
    {
        var dto = await CreateSupportTicketCommandHandler.ProjectTicketById(
            query.TicketId,
            _db.SupportTickets.AsNoTracking().Where(t => t.RequesterUserID == query.UserId),
            ct);

        return dto == null
            ? Result<SupportTicketResponseDto>.Failure("Support ticket not found.", "SUPPORT_TICKET_NOT_FOUND")
            : Result<SupportTicketResponseDto>.Success(dto);
    }
}
