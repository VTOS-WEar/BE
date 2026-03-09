using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>UC 3.9.18 — Reject a production order with a reason.</summary>
public record RejectProductionOrderCommand(Guid UserId, Guid BatchId, string Reason);

public interface IRejectProductionOrderCommandHandler
{
    Task<Result<string>> HandleAsync(RejectProductionOrderCommand command, CancellationToken ct = default);
}

public class RejectProductionOrderCommandHandler : IRejectProductionOrderCommandHandler
{
    private readonly IApplicationDbContext _db;

    public RejectProductionOrderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(RejectProductionOrderCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result<string>.Failure("Rejection reason is required.", "REASON_REQUIRED");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user?.SchoolID == null)
            return Result<string>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .Include(b => b.Campaign)
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.Campaign.SchoolID == user.SchoolID.Value
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
