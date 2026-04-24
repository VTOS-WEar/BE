using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

/// <summary>
/// Provider responds to a complaint.
/// Sets Response + RespondedAt + transitions status (Open → InProgress).
/// If markResolved=true, also transitions InProgress → Resolved.
/// </summary>
public record RespondSupportTicketCommand(Guid UserId, Guid ComplaintId, string Response, bool MarkResolved = false);

public interface IRespondSupportTicketCommandHandler
{
    Task<Result<string>> HandleAsync(RespondSupportTicketCommand command, CancellationToken ct = default);
}

public class RespondSupportTicketCommandHandler : IRespondSupportTicketCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public RespondSupportTicketCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<string>> HandleAsync(RespondSupportTicketCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<string>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<string>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(c => c.Id == command.ComplaintId && c.ProviderID == providerMgr.ProviderID, ct);

        if (ticket == null)
            return Result<string>.Failure("SupportTicket not found.", "COMPLAINT_NOT_FOUND");

        if (ticket.Status == SupportTicketStatus.Closed)
            return Result<string>.Failure("Cannot respond to a closed complaint.", "COMPLAINT_CLOSED");

        if (string.IsNullOrWhiteSpace(command.Response))
            return Result<string>.Failure("Response is required.", "RESPONSE_REQUIRED");

        ticket.Response = command.Response;
        ticket.RespondedAt = DateTime.UtcNow;

        if (ticket.Status == SupportTicketStatus.Open)
            ticket.Status = SupportTicketStatus.InProgress;

        if (command.MarkResolved && ticket.Status == SupportTicketStatus.InProgress)
        {
            ticket.Status = SupportTicketStatus.Resolved;
            ticket.ResolvedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        // Notify school about complaint response
        try
        {
            if (ticket.SchoolID.HasValue)
            {
                var statusMsg = command.MarkResolved ? "và đánh dấu đã giải quyết" : "";
                await _notificationService.NotifySchoolAsync(ticket.SchoolID.Value,
                    "💬 NCC phản hồi khiếu nại",
                    $"NCC đã phản hồi khiếu nại: {ticket.Title} {statusMsg}".Trim(),
                    "SupportTicket", ticket.Id, "SupportTicket",
                    "/school/complaints", ct);
            }
        }
        catch { /* Don't fail */ }

        var msg = command.MarkResolved ? "SupportTicket responded and resolved." : "SupportTicket responded.";
        return Result<string>.Success(msg);
    }
}
