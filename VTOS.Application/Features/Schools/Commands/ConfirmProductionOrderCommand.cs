using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>UC 3.9.10 — School confirms a production order (batch moves to Approved).</summary>
public record ConfirmProductionOrderCommand(Guid UserId, Guid BatchId);

public interface IConfirmProductionOrderCommandHandler
{
    Task<Result<string>> HandleAsync(ConfirmProductionOrderCommand command, CancellationToken ct = default);
}

public class ConfirmProductionOrderCommandHandler : IConfirmProductionOrderCommandHandler
{
    private readonly IApplicationDbContext _db;

    public ConfirmProductionOrderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(ConfirmProductionOrderCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user?.SchoolID == null)
            return Result<string>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .Include(b => b.Campaign)
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.Campaign.SchoolID == user.SchoolID.Value
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<string>.Failure("Production batch not found.", "BATCH_NOT_FOUND");

        if (batch.Status != ProductionBatchStatus.Pending)
            return Result<string>.Failure(
                $"Only Pending batches can be confirmed. Current: {batch.Status}.", "INVALID_STATUS");

        batch.Status = ProductionBatchStatus.Approved;
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success("Production order confirmed. Provider can now begin production.");
    }
}
