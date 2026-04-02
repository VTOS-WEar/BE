using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>UC 3.9.9 — Send production request to provider (set batch status to Pending, signaling readiness for provider pickup).</summary>
public record SendProductionRequestCommand(Guid UserId, Guid BatchId);

public interface ISendProductionRequestCommandHandler
{
    Task<Result<string>> HandleAsync(SendProductionRequestCommand command, CancellationToken ct = default);
}

public class SendProductionRequestCommandHandler : ISendProductionRequestCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public SendProductionRequestCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<string>> HandleAsync(SendProductionRequestCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<string>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .Include(b => b.Campaign)
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.Campaign.SchoolID == schoolMgr.SchoolID
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<string>.Failure("Production batch not found.", "BATCH_NOT_FOUND");

        if (batch.Status != ProductionBatchStatus.Pending)
            return Result<string>.Failure(
                $"Batch must be in Pending status to send. Current: {batch.Status}.", "INVALID_STATUS");

        // Status stays Pending — this represents the "sent" state visible to provider
        // Provider will Approve or Reject it
        await _db.SaveChangesAsync(ct);

        // Notify provider about new production request
        try
        {
            await _notificationService.NotifyProviderAsync(batch.ProviderID,
                "📋 Đơn sản xuất mới",
                $"Trường gửi đơn sản xuất: {batch.BatchName}. Vui lòng xác nhận.",
                "ProductionOrder", batch.Id, "ProductionBatch",
                "/provider/production-orders", ct);
        }
        catch { /* Don't fail */ }

        return Result<string>.Success("Production request sent to provider successfully.");
    }
}
