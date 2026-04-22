using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>School reports defective uniforms (creates a SupportTicket linked to the batch).</summary>
public record ReportDefectCommand(Guid UserId, Guid BatchId, string Title, string Description, List<string>? ProofImageUrls);

public interface IReportDefectCommandHandler
{
    Task<Result<Guid>> HandleAsync(ReportDefectCommand command, CancellationToken ct = default);
}

public class ReportDefectCommandHandler : IReportDefectCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public ReportDefectCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<Guid>> HandleAsync(ReportDefectCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<Guid>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Include(b => b.Campaign)
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.Campaign.SchoolID == schoolMgr.SchoolID
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<Guid>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        if (string.IsNullOrWhiteSpace(command.Title))
            return Result<Guid>.Failure("Title is required.", "TITLE_REQUIRED");

        if (string.IsNullOrWhiteSpace(command.Description))
            return Result<Guid>.Failure("Description is required.", "DESCRIPTION_REQUIRED");

        // Require at least 1 proof image
        if (command.ProofImageUrls == null || !command.ProofImageUrls.Any())
            return Result<Guid>.Failure("At least one proof image is required.", "PROOF_REQUIRED");

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            CampaignID = batch.CampaignID,
            BatchID = batch.Id,
            SchoolID = schoolMgr.SchoolID,
            ProviderID = batch.ProviderID,
            Title = command.Title,
            Description = command.Description,
            ProofImageUrls = JsonSerializer.Serialize(command.ProofImageUrls),
            Status = SupportTicketStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        // Notify provider about new complaint
        try
        {
            await _notificationService.NotifyProviderAsync(batch.ProviderID,
                "⚠️ Khiếu nại mới",
                $"Trường khiếu nại đơn {batch.BatchName}: {command.Title}",
                "SupportTicket", ticket.Id, "SupportTicket",
                "/provider/complaints", ct);
        }
        catch { /* Don't fail */ }

        // Also notify admins
        try
        {
            await _notificationService.NotifyAdminsAsync(
                "⚠️ Khiếu nại mới",
                $"Trường khiếu nại đơn {batch.BatchName}: {command.Title}",
                "SupportTicket", ticket.Id, "SupportTicket",
                "/admin/complaints", ct);
        }
        catch { /* Don't fail */ }

        return Result<Guid>.Success(ticket.Id);
    }
}
