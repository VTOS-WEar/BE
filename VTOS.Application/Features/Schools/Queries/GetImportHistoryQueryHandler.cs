using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

public class GetImportHistoryQueryHandler : IGetImportHistoryQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetImportHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ImportBatchDto>>> HandleAsync(GetImportHistoryQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null)
            return Result<IReadOnlyList<ImportBatchDto>>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");


        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);


        var schoolId = schoolMgr.SchoolID;

        var batches = await _db.ImportBatches
            .AsNoTracking()
            .Where(b => b.SchoolID == schoolId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(query.Limit)
            .Select(b => new ImportBatchDto
            {
                Id = b.Id,
                FileName = b.FileName,
                TotalRows = b.TotalRows,
                SuccessCount = b.SuccessCount,
                SkippedCount = b.SkippedCount,
                ErrorCount = b.ErrorCount,
                CreatedAt = b.CreatedAt,
                Status = b.ErrorCount > 0 ? "error" : "success",
            })
            .ToListAsync(ct);

        return Result<IReadOnlyList<ImportBatchDto>>.Success(batches);
    }
}
