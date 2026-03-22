using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Distribution;

// ── DTOs ──

public record DistributionScheduleDto
{
    public Guid Id { get; init; }
    public Guid BatchId { get; init; }
    public DateTime ScheduledDate { get; init; }
    public string Method { get; init; } = string.Empty;
    public string TimeSlot { get; init; } = string.Empty;
    public string? Note { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

// ── Create ──

public record CreateDistributionScheduleCommand(Guid UserId, Guid BatchId, DateTime ScheduledDate, string Method, string TimeSlot, string? Note);

public interface ICreateDistributionScheduleHandler
{
    Task<Result<Guid>> HandleAsync(CreateDistributionScheduleCommand cmd, CancellationToken ct = default);
}

public class CreateDistributionScheduleHandler : ICreateDistributionScheduleHandler
{
    private readonly IApplicationDbContext _db;
    public CreateDistributionScheduleHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> HandleAsync(CreateDistributionScheduleCommand cmd, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user!.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<Guid>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var batch = await _db.ProductionBatches
            .Include(b => b.Campaign)
            .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.Campaign.SchoolID == schoolMgr.SchoolID && !b.IsDeleted, ct);
        if (batch == null)
            return Result<Guid>.Failure("Batch not found.", "BATCH_NOT_FOUND");

        var schedule = new DistributionSchedule
        {
            Id = Guid.NewGuid(),
            BatchID = cmd.BatchId,
            ScheduledDate = cmd.ScheduledDate,
            Method = cmd.Method,
            TimeSlot = cmd.TimeSlot,
            Note = cmd.Note,
            Status = "Planned",
            CreatedAt = DateTime.UtcNow
        };

        _db.DistributionSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(schedule.Id);
    }
}

// ── List ──

public interface IGetDistributionSchedulesHandler
{
    Task<Result<List<DistributionScheduleDto>>> HandleAsync(Guid userId, Guid batchId, CancellationToken ct = default);
}

public class GetDistributionSchedulesHandler : IGetDistributionSchedulesHandler
{
    private readonly IApplicationDbContext _db;
    public GetDistributionSchedulesHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<DistributionScheduleDto>>> HandleAsync(Guid userId, Guid batchId, CancellationToken ct = default)
    {
        var items = await _db.DistributionSchedules
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

        return Result<List<DistributionScheduleDto>>.Success(items);
    }
}

// ── Update ──

public record UpdateDistributionScheduleCommand(Guid ScheduleId, DateTime? ScheduledDate, string? Method, string? TimeSlot, string? Note, string? Status);

public interface IUpdateDistributionScheduleHandler
{
    Task<Result<string>> HandleAsync(Guid userId, UpdateDistributionScheduleCommand cmd, CancellationToken ct = default);
}

public class UpdateDistributionScheduleHandler : IUpdateDistributionScheduleHandler
{
    private readonly IApplicationDbContext _db;
    public UpdateDistributionScheduleHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(Guid userId, UpdateDistributionScheduleCommand cmd, CancellationToken ct = default)
    {
        var schedule = await _db.DistributionSchedules.FirstOrDefaultAsync(s => s.Id == cmd.ScheduleId, ct);
        if (schedule == null)
            return Result<string>.Failure("Schedule not found.", "NOT_FOUND");

        if (cmd.ScheduledDate.HasValue) schedule.ScheduledDate = cmd.ScheduledDate.Value;
        if (!string.IsNullOrEmpty(cmd.Method)) schedule.Method = cmd.Method;
        if (!string.IsNullOrEmpty(cmd.TimeSlot)) schedule.TimeSlot = cmd.TimeSlot;
        if (cmd.Note != null) schedule.Note = cmd.Note;
        if (!string.IsNullOrEmpty(cmd.Status))
        {
            schedule.Status = cmd.Status;
            if (cmd.Status == "Completed") schedule.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Result<string>.Success("Schedule updated.");
    }
}
