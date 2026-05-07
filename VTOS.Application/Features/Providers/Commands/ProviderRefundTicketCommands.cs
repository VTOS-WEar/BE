using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

public record ApproveComplaintRefundCommand(Guid UserId, Guid ComplaintId, string? Note);

public record ApproveComplaintRefundResponse(string Message, Guid RefundId, string RefundStatus);

public interface IApproveComplaintRefundCommandHandler
{
    Task<Result<ApproveComplaintRefundResponse>> HandleAsync(ApproveComplaintRefundCommand command, CancellationToken ct = default);
}

public record ForwardComplaintToAdminCommand(Guid UserId, Guid ComplaintId, string Note);

public interface IForwardComplaintToAdminCommandHandler
{
    Task<Result<string>> HandleAsync(ForwardComplaintToAdminCommand command, CancellationToken ct = default);
}

public class ProviderRefundTicketCommandHandler :
    IApproveComplaintRefundCommandHandler,
    IForwardComplaintToAdminCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public ProviderRefundTicketCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<ApproveComplaintRefundResponse>> HandleAsync(
        ApproveComplaintRefundCommand command,
        CancellationToken ct = default)
    {
        var context = await LoadTicketContextAsync(command.UserId, command.ComplaintId, ct);
        if (!context.IsSuccess)
            return Result<ApproveComplaintRefundResponse>.Failure(context.Error!, context.ErrorCode);

        var ticket = context.Value!.Ticket;
        var order = context.Value.Order;

        var originalPayment = order.PaymentTransactions
            .FirstOrDefault(p => p.TransactionType == TransactionType.OrderPayment
                && p.TransactionStatus == PaymentStatus.Completed);

        if (originalPayment == null)
            return Result<ApproveComplaintRefundResponse>.Failure("No completed payment found for this order.", "NO_PAYMENT");

        var existingRefund = await _db.Refunds
            .Where(r => r.PaymentID == originalPayment.Id
                && (r.RefundStatus == RefundStatus.Pending
                    || r.RefundStatus == RefundStatus.Processing
                    || r.RefundStatus == RefundStatus.Completed))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existingRefund != null)
            return Result<ApproveComplaintRefundResponse>.Failure("A refund request already exists for this order.", "REFUND_ALREADY_EXISTS");

        var now = DateTime.UtcNow;
        var note = string.IsNullOrWhiteSpace(command.Note)
            ? "Provider đồng ý hoàn tiền."
            : command.Note.Trim();

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentID = originalPayment.Id,
            RefundAmount = order.TotalAmount,
            RefundStatus = RefundStatus.Pending,
            DisputeReason = BuildRefundReason(ticket, note),
            CreatedAt = now,
            CreatedBy = "Provider"
        };

        _db.Refunds.Add(refund);
        AppendProviderNote(ticket, note);
        ticket.Status = SupportTicketStatus.Resolved;
        ticket.RespondedAt = now;
        ticket.ResolvedAt = now;
        ticket.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);

        await NotifyRefundReviewAsync(ticket, order, ct);

        return Result<ApproveComplaintRefundResponse>.Success(
            new ApproveComplaintRefundResponse("Refund request created and waiting for review.", refund.Id, refund.RefundStatus.ToString()));
    }

    public async Task<Result<string>> HandleAsync(
        ForwardComplaintToAdminCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Note))
            return Result<string>.Failure("Forward note is required.", "NOTE_REQUIRED");

        var context = await LoadTicketContextAsync(command.UserId, command.ComplaintId, ct);
        if (!context.IsSuccess)
            return Result<string>.Failure(context.Error!, context.ErrorCode);

        var ticket = context.Value!.Ticket;
        AppendProviderNote(ticket, command.Note.Trim());
        ticket.Status = SupportTicketStatus.InProgress;
        ticket.RespondedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        try
        {
            await _notificationService.NotifyAdminsAsync(
                "Provider chuyển ticket cần xử lý",
                $"Provider chuyển ticket cho Admin xử lý: {ticket.Title}",
                "SupportTicket",
                ticket.Id,
                "SupportTicket",
                "/admin/complaints",
                ct);
        }
        catch
        {
            // Ticket forwarding should not fail if notifications are unavailable.
        }

        return Result<string>.Success("Support ticket forwarded to Admin.");
    }

    private async Task<Result<TicketContext>> LoadTicketContextAsync(Guid userId, Guid complaintId, CancellationToken ct)
    {
        var providerId = await _db.ProviderManagers
            .AsNoTracking()
            .Where(m => m.UserID == userId)
            .Select(m => (Guid?)m.ProviderID)
            .FirstOrDefaultAsync(ct);

        if (!providerId.HasValue)
            return Result<TicketContext>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var ticket = await _db.SupportTickets
            .Include(t => t.Order)
                .ThenInclude(o => o!.PaymentTransactions)
            .Include(t => t.Order)
                .ThenInclude(o => o!.ChildProfile)
            .FirstOrDefaultAsync(t => t.Id == complaintId
                && t.ProviderID == providerId.Value
                && t.RequesterRole != "Provider", ct);

        if (ticket == null)
            return Result<TicketContext>.Failure("SupportTicket not found.", "COMPLAINT_NOT_FOUND");

        if (ticket.Status == SupportTicketStatus.Closed)
            return Result<TicketContext>.Failure("Cannot process a closed complaint.", "COMPLAINT_CLOSED");

        if (!ticket.OrderID.HasValue || ticket.Order == null)
            return Result<TicketContext>.Failure("Ticket is not linked to an order.", "ORDER_NOT_FOUND");

        if (ticket.Order.ProviderID != providerId.Value)
            return Result<TicketContext>.Failure("Order does not belong to this provider.", "ORDER_NOT_FOUND");

        return Result<TicketContext>.Success(new TicketContext(ticket, ticket.Order));
    }

    private static void AppendProviderNote(SupportTicket ticket, string note)
    {
        var providerNote = $"[Provider - {DateTime.UtcNow:dd/MM/yyyy HH:mm}] {note}";
        ticket.Response = string.IsNullOrWhiteSpace(ticket.Response)
            ? providerNote
            : ticket.Response + "\n\n" + providerNote;
    }

    private static string BuildRefundReason(SupportTicket ticket, string note)
    {
        return $"Ticket #{ticket.Id.ToString()[..8]} - {ticket.Title}. Provider note: {note}";
    }

    private async Task NotifyRefundReviewAsync(SupportTicket ticket, Order order, CancellationToken ct)
    {
        try
        {
            await _notificationService.NotifyAdminsAsync(
                "Provider đồng ý hoàn tiền",
                $"Provider đã đồng ý hoàn tiền cho ticket: {ticket.Title}",
                "SupportTicket",
                ticket.Id,
                "SupportTicket",
                "/admin/complaints",
                ct);

            var schoolId = ticket.SchoolID ?? (Guid?)order.ChildProfile.SchoolID;
            if (schoolId.HasValue)
            {
                await _notificationService.NotifySchoolAsync(
                    schoolId.Value,
                    "Có yêu cầu hoàn tiền cần duyệt",
                    $"Provider đã đồng ý hoàn tiền cho đơn #{order.Id.ToString()[..8]}.",
                    "Refund",
                    ticket.Id,
                    "SupportTicket",
                    "/school/refunds",
                    ct);
            }
        }
        catch
        {
            // Refund creation should not fail because notification delivery failed.
        }
    }

    private sealed record TicketContext(SupportTicket Ticket, Order Order);
}
