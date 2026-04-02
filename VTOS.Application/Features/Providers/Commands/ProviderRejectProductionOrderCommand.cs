using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

/// <summary>Provider rejects a production order with a reason.</summary>
public record ProviderRejectProductionOrderCommand(Guid UserId, Guid BatchId, string Reason);

public interface IProviderRejectProductionOrderCommandHandler
{
    Task<Result<string>> HandleAsync(ProviderRejectProductionOrderCommand command, CancellationToken ct = default);
}

public class ProviderRejectProductionOrderCommandHandler : IProviderRejectProductionOrderCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public ProviderRejectProductionOrderCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<string>> HandleAsync(ProviderRejectProductionOrderCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result<string>.Failure("Rejection reason is required.", "REASON_REQUIRED");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<string>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.ProviderID == providerMgr.ProviderID
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<string>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        if (batch.Status is ProductionBatchStatus.Completed or ProductionBatchStatus.Rejected or ProductionBatchStatus.Delivered)
            return Result<string>.Failure(
                $"Cannot reject a batch in {batch.Status} status.", "INVALID_STATUS");

        batch.Status = ProductionBatchStatus.Rejected;
        batch.RejectionReason = command.Reason;
        await _db.SaveChangesAsync(ct);

        // Notify school
        try
        {
            var campaign = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == batch.CampaignID, ct);
            if (campaign != null)
                await _notificationService.NotifySchoolAsync(campaign.SchoolID,
                    "❌ NCC từ chối đơn sản xuất",
                    $"NCC đã từ chối đơn {batch.BatchName}. Lý do: {command.Reason}",
                    "ProductionOrder", batch.Id, "ProductionBatch",
                    "/school/production-orders", ct);
        }
        catch { /* Don't fail */ }

        return Result<string>.Success("Production order rejected.");
    }
}
