using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>UC 3.9.17 — Process (mark as InProduction) a confirmed production order.</summary>
public record ProcessProductionOrderCommand(Guid UserId, Guid BatchId);

public interface IProcessProductionOrderCommandHandler
{
    Task<Result<string>> HandleAsync(ProcessProductionOrderCommand command, CancellationToken ct = default);
}

public class ProcessProductionOrderCommandHandler : IProcessProductionOrderCommandHandler
{
    private readonly IApplicationDbContext _db;

    public ProcessProductionOrderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(ProcessProductionOrderCommand command, CancellationToken ct = default)
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
            return Result<string>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        if (batch.Status != ProductionBatchStatus.Approved)
            return Result<string>.Failure(
                $"Only Approved batches can be processed. Current: {batch.Status}.", "INVALID_STATUS");

        batch.Status = ProductionBatchStatus.InProduction;
        batch.ProcessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success("Production order is now InProduction.");
    }
}
