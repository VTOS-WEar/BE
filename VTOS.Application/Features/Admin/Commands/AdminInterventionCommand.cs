using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

// ── Command ──

public record AdminInterventionCommand(Guid ComplaintId, string Note, string? Action);

// ── Interface ──

public interface IAdminInterventionCommandHandler
{
    Task<Result<string>> HandleAsync(AdminInterventionCommand command, CancellationToken ct = default);
}

// ── Handler ──

public class AdminInterventionCommandHandler : IAdminInterventionCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public AdminInterventionCommandHandler(IApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<string>> HandleAsync(AdminInterventionCommand command, CancellationToken ct = default)
    {
        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(c => c.Id == command.ComplaintId, ct);

        if (ticket == null)
            return Result<string>.Failure("SupportTicket not found.", "NOT_FOUND");

        // Admin can add note (append to response), change status
        var adminNote = $"[Admin - {DateTime.UtcNow:dd/MM/yyyy HH:mm}] {command.Note}";
        ticket.Response = string.IsNullOrEmpty(ticket.Response)
            ? adminNote
            : ticket.Response + "\n\n" + adminNote;

        if (!string.IsNullOrEmpty(command.Action))
        {
            switch (command.Action.ToLower())
            {
                case "escalate":
                    ticket.Status = SupportTicketStatus.InProgress;
                    break;
                case "resolve":
                    ticket.Status = SupportTicketStatus.Resolved;
                    ticket.ResolvedAt = DateTime.UtcNow;
                    break;
                case "close":
                    ticket.Status = SupportTicketStatus.Closed;
                    ticket.ResolvedAt ??= DateTime.UtcNow;
                    break;
            }
        }

        ticket.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Notify both school and provider about admin intervention
        try
        {
            var statusLabel = command.Action?.ToLower() switch
            {
                "resolve" => "Giải quyết",
                "close" => "Đóng",
                "escalate" => "Chuyển tiếp",
                _ => "Ghi chú"
            };
            if (ticket.SchoolID != Guid.Empty)
                await _notificationService.NotifySchoolAsync(ticket.SchoolID,
                    "👨‍⚖️ Admin can thiệp khiếu nại",
                    $"Admin {statusLabel} khiếu nại: {ticket.Title}",
                    "SupportTicket", ticket.Id, "SupportTicket",
                    "/school/complaints", ct);
            if (ticket.ProviderID.HasValue)
                await _notificationService.NotifyProviderAsync(ticket.ProviderID.Value,
                    "👨‍⚖️ Admin can thiệp khiếu nại",
                    $"Admin {statusLabel} khiếu nại: {ticket.Title}",
                    "SupportTicket", ticket.Id, "SupportTicket",
                    "/provider/complaints", ct);
        }
        catch { /* Don't fail */ }

        return Result<string>.Success($"Intervention added. Status: {ticket.Status}");
    }
}
