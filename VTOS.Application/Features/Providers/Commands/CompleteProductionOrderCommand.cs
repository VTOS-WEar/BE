using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

/// <summary>Provider marks a production order as Completed (InProduction → Completed).</summary>
public record CompleteProductionOrderCommand(Guid UserId, Guid BatchId);

public interface ICompleteProductionOrderCommandHandler
{
    Task<Result<string>> HandleAsync(CompleteProductionOrderCommand command, CancellationToken ct = default);
}

public class CompleteProductionOrderCommandHandler : ICompleteProductionOrderCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public CompleteProductionOrderCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<string>> HandleAsync(CompleteProductionOrderCommand command, CancellationToken ct = default)
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

        if (batch.Status != ProductionBatchStatus.InProduction)
            return Result<string>.Failure(
                $"Only InProduction batches can be completed. Current: {batch.Status}.", "INVALID_STATUS");

        batch.Status = ProductionBatchStatus.Completed;
        await _db.SaveChangesAsync(ct);

        // Notify school
        try
        {
            var campaign = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == batch.CampaignID, ct);
            if (campaign != null)
                await _notificationService.NotifySchoolAsync(campaign.SchoolID,
                    "🏭 Đơn sản xuất hoàn thành",
                    $"Đơn sản xuất {batch.BatchName} đã hoàn thành. Sẵn sàng giao hàng.",
                    "ProductionOrder", batch.Id, "ProductionBatch",
                    "/school/production-orders", ct);
        }
        catch { /* Don't fail */ }

        return Result<string>.Success("Production order completed successfully.");
    }
}
