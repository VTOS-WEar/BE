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
public record RespondComplaintCommand(Guid UserId, Guid ComplaintId, string Response, bool MarkResolved = false);

public interface IRespondComplaintCommandHandler
{
    Task<Result<string>> HandleAsync(RespondComplaintCommand command, CancellationToken ct = default);
}

public class RespondComplaintCommandHandler : IRespondComplaintCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public RespondComplaintCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<string>> HandleAsync(RespondComplaintCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<string>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var complaint = await _db.Complaints
            .FirstOrDefaultAsync(c => c.Id == command.ComplaintId && c.ProviderID == providerMgr.ProviderID, ct);

        if (complaint == null)
            return Result<string>.Failure("Complaint not found.", "COMPLAINT_NOT_FOUND");

        if (complaint.Status == ComplaintStatus.Closed)
            return Result<string>.Failure("Cannot respond to a closed complaint.", "COMPLAINT_CLOSED");

        if (string.IsNullOrWhiteSpace(command.Response))
            return Result<string>.Failure("Response is required.", "RESPONSE_REQUIRED");

        complaint.Response = command.Response;
        complaint.RespondedAt = DateTime.UtcNow;

        if (complaint.Status == ComplaintStatus.Open)
            complaint.Status = ComplaintStatus.InProgress;

        if (command.MarkResolved && complaint.Status == ComplaintStatus.InProgress)
        {
            complaint.Status = ComplaintStatus.Resolved;
            complaint.ResolvedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        // Notify school about complaint response
        try
        {
            if (complaint.SchoolID != Guid.Empty)
            {
                var statusMsg = command.MarkResolved ? "và đánh dấu đã giải quyết" : "";
                await _notificationService.NotifySchoolAsync(complaint.SchoolID,
                    "💬 NCC phản hồi khiếu nại",
                    $"NCC đã phản hồi khiếu nại: {complaint.Title} {statusMsg}".Trim(),
                    "Complaint", complaint.Id, "Complaint",
                    "/school/complaints", ct);
            }
        }
        catch { /* Don't fail */ }

        var msg = command.MarkResolved ? "Complaint responded and resolved." : "Complaint responded.";
        return Result<string>.Success(msg);
    }
}
