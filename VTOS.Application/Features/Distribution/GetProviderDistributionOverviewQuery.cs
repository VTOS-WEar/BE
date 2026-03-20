using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Distribution;

// ── DTOs ──

public record ProviderDistributionOverviewDto
{
    public Guid BatchId { get; init; }
    public int TotalOrders { get; init; }
    public int DistributedCount { get; init; }
    public int PendingCount { get; init; }
    public int AtSchoolCount { get; init; }
    public int AtHomeCount { get; init; }
    public List<DistributionScheduleDto> Schedules { get; init; } = new();
}

// ── Interface ──

public interface IGetProviderDistributionOverviewHandler
{
    Task<Result<ProviderDistributionOverviewDto>> HandleAsync(Guid userId, Guid batchId, CancellationToken ct = default);
}

// ── Handler ──

public class GetProviderDistributionOverviewHandler : IGetProviderDistributionOverviewHandler
{
    private readonly IApplicationDbContext _db;
    public GetProviderDistributionOverviewHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ProviderDistributionOverviewDto>> HandleAsync(Guid userId, Guid batchId, CancellationToken ct = default)
    {
        var batch = await _db.ProductionBatches
            .Include(b => b.Campaign)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted, ct);

        if (batch == null)
            return Result<ProviderDistributionOverviewDto>.Failure("Batch not found.", "NOT_FOUND");

        // Total orders in campaign
        var totalOrders = await _db.Orders
            .CountAsync(o => o.CampaignID == batch.CampaignID
                && o.OrderStatus != Domain.Enums.OrderStatus.Cancelled, ct);

        // Distribution records
        var distributions = await _db.DistributionRecords
            .Where(dr => dr.BatchID == batchId)
            .ToListAsync(ct);

        var distributedCount = distributions.Count;
        var atSchool = distributions.Count(d => d.Method == "AtSchool");
        var atHome = distributions.Count(d => d.Method == "AtHome");

        // Schedules (read-only for provider)
        var schedules = await _db.DistributionSchedules
            .Where(s => s.BatchID == batchId)
            .OrderBy(s => s.ScheduledDate)
            .Select(s => new DistributionScheduleDto
            {
                Id = s.Id, BatchId = s.BatchID,
                ScheduledDate = s.ScheduledDate, Method = s.Method,
                TimeSlot = s.TimeSlot, Note = s.Note, Status = s.Status,
                CreatedAt = s.CreatedAt, CompletedAt = s.CompletedAt
            })
            .ToListAsync(ct);

        return Result<ProviderDistributionOverviewDto>.Success(new ProviderDistributionOverviewDto
        {
            BatchId = batchId,
            TotalOrders = totalOrders,
            DistributedCount = distributedCount,
            PendingCount = totalOrders - distributedCount,
            AtSchoolCount = atSchool,
            AtHomeCount = atHome,
            Schedules = schedules
        });
    }
}
