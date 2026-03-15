using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
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

    public ProviderRejectProductionOrderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(ProviderRejectProductionOrderCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result<string>.Failure("Rejection reason is required.", "REASON_REQUIRED");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user?.ProviderID == null)
            return Result<string>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.ProviderID == user.ProviderID.Value
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<string>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        if (batch.Status is ProductionBatchStatus.Completed or ProductionBatchStatus.Rejected or ProductionBatchStatus.Delivered)
            return Result<string>.Failure(
                $"Cannot reject a batch in {batch.Status} status.", "INVALID_STATUS");

        batch.Status = ProductionBatchStatus.Rejected;
        batch.RejectionReason = command.Reason;
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success("Production order rejected.");
    }
}
