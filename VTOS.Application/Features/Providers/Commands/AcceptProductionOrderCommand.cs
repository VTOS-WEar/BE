using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

/// <summary>Provider accepts a production order → status moves to InProduction.</summary>
public record AcceptProductionOrderCommand(Guid UserId, Guid BatchId);

public interface IAcceptProductionOrderCommandHandler
{
    Task<Result<string>> HandleAsync(AcceptProductionOrderCommand command, CancellationToken ct = default);
}

public class AcceptProductionOrderCommandHandler : IAcceptProductionOrderCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public AcceptProductionOrderCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<string>> HandleAsync(AcceptProductionOrderCommand command, CancellationToken ct = default)
    {
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

        if (batch.Status != ProductionBatchStatus.Approved)
            return Result<string>.Failure(
                $"Only Approved batches can be accepted. Current: {batch.Status}.", "INVALID_STATUS");

        batch.Status = ProductionBatchStatus.InProduction;
        batch.ProcessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Notify school
        try
        {
            var campaign = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == batch.CampaignID, ct);
            if (campaign != null)
                await _notificationService.NotifySchoolAsync(campaign.SchoolID,
                    "✅ Đơn sản xuất đã nhận",
                    $"NCC đã nhận đơn sản xuất: {batch.BatchName}. Trạng thái: Đang sản xuất.",
                    "ProductionOrder", batch.Id, "ProductionBatch",
                    "/school/production-orders", ct);
        }
        catch { /* Don't fail */ }

        return Result<string>.Success("Production order accepted. Status is now InProduction.");
    }
}
