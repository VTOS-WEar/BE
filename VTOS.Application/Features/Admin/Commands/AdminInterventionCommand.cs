using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public record AdminInterventionCommand(Guid ComplaintId, string Note, string? Action);

public interface IAdminInterventionCommandHandler
{
    Task<Result<string>> HandleAsync(AdminInterventionCommand command, CancellationToken ct = default);
}

public class AdminInterventionCommandHandler : IAdminInterventionCommandHandler
{
    private const string OrderCancellationCategory = "OrderCancellation";

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

        if (string.IsNullOrWhiteSpace(command.Note))
            return Result<string>.Failure("Admin note is required.", "NOTE_REQUIRED");

        var adminNote = $"[Admin - {DateTime.UtcNow:dd/MM/yyyy HH:mm}] {command.Note.Trim()}";
        ticket.Response = string.IsNullOrEmpty(ticket.Response)
            ? adminNote
            : ticket.Response + "\n\n" + adminNote;

        var action = command.Action?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(action))
        {
            switch (action)
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

        if (ticket.Category == OrderCancellationCategory
            && ticket.OrderID.HasValue
            && (action == "resolve" || action == "close"))
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == ticket.OrderID.Value, ct);

            if (order == null)
                return Result<string>.Failure("Linked order not found.", "ORDER_NOT_FOUND");

            if (action == "resolve")
            {
                if (order.OrderStatus != OrderStatus.CancellationRequested
                    && order.OrderStatus != OrderStatus.Paid
                    && order.OrderStatus != OrderStatus.Cancelled)
                    return Result<string>.Failure("Order is no longer waiting for cancellation review.", "INVALID_ORDER_STATUS");

                order.OrderStatus = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
            }
            else if (action == "close" && order.OrderStatus == OrderStatus.CancellationRequested)
            {
                order.OrderStatus = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;
            }
        }

        ticket.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        try
        {
            var statusLabel = command.Action?.ToLowerInvariant() switch
            {
                "resolve" => "giai quyet",
                "close" => "dong",
                "escalate" => "chuyen tiep",
                _ => "ghi chu"
            };

            if (ticket.RequesterUserID.HasValue)
            {
                await _notificationService.CreateAsync(
                    ticket.RequesterUserID.Value,
                    "Admin da cap nhat yeu cau ho tro",
                    $"Admin da {statusLabel} yeu cau ho tro: {ticket.Title}",
                    "SupportTicket",
                    ticket.Id,
                    "SupportTicket",
                    ResolveRequesterSupportUrl(ticket.RequesterRole),
                    ct);
            }
            else if (ticket.SchoolID.HasValue)
            {
                await _notificationService.NotifySchoolAsync(
                    ticket.SchoolID.Value,
                    "Admin can thiep khieu nai",
                    $"Admin da {statusLabel} khieu nai: {ticket.Title}",
                    "SupportTicket",
                    ticket.Id,
                    "SupportTicket",
                    "/school/complaints",
                    ct);
            }

            if (!ticket.RequesterUserID.HasValue && ticket.ProviderID.HasValue)
            {
                await _notificationService.NotifyProviderAsync(
                    ticket.ProviderID.Value,
                    "Admin can thiep khieu nai",
                    $"Admin da {statusLabel} khieu nai: {ticket.Title}",
                    "SupportTicket",
                    ticket.Id,
                    "SupportTicket",
                    "/provider/complaints",
                    ct);
            }
        }
        catch
        {
            // Admin handling should not fail because notification delivery failed.
        }

        return Result<string>.Success($"Intervention added. Status: {ticket.Status}");
    }

    private static string ResolveRequesterSupportUrl(string? requesterRole)
    {
        return requesterRole?.ToLowerInvariant() switch
        {
            "parent" => "/parentprofile/support",
            "provider" => "/provider/support",
            "school" => "/school/support",
            "homeroomteacher" => "/teacher/support",
            _ => "/"
        };
    }
}
