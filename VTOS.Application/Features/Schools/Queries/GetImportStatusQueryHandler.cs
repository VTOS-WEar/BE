using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public class GetImportStatusQueryHandler : IGetImportStatusQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetImportStatusQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ImportStatusDto>> HandleAsync(GetImportStatusQuery query, CancellationToken ct = default)
    {
        var schoolMgr = await _db.SchoolManagers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == query.UserId, ct);

        if (schoolMgr == null)
            return Result<ImportStatusDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolId = schoolMgr.SchoolID;

        // ── Determine current semester ──
        // Vietnamese academic year: HK1 = Aug–Jan, HK2 = Feb–Jun, Summer = Jul
        var now = DateTime.UtcNow;
        string currentSemester;
        string suggestedDeadline;
        DateTime semesterStart;

        if (now.Month >= 8) // Aug–Dec → HK1 of current/next year
        {
            currentSemester = $"Học kỳ 1 ({now.Year}-{now.Year + 1})";
            suggestedDeadline = $"30/08/{now.Year}";
            semesterStart = new DateTime(now.Year, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        else if (now.Month >= 2) // Feb–Jul → HK2 of prev/current year
        {
            currentSemester = $"Học kỳ 2 ({now.Year - 1}-{now.Year})";
            suggestedDeadline = $"15/02/{now.Year}";
            semesterStart = new DateTime(now.Year, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        else // Jan → still HK1 of prev/current year
        {
            currentSemester = $"Học kỳ 1 ({now.Year - 1}-{now.Year})";
            suggestedDeadline = $"30/08/{now.Year - 1}";
            semesterStart = new DateTime(now.Year - 1, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        // ── Check last successful import since semester start ──
        var lastImport = await _db.ImportBatches
            .AsNoTracking()
            .Where(b => b.SchoolID == schoolId && b.CreatedAt >= semesterStart && b.SuccessCount > 0)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // ── Count active students ──
        var studentCount = await _db.StudentDataImports
            .AsNoTracking()
            .CountAsync(s => s.SchoolID == schoolId, ct);

        var needsUpdate = lastImport == null;

        return Result<ImportStatusDto>.Success(new ImportStatusDto
        {
            NeedsUpdate = needsUpdate,
            CurrentSemester = currentSemester,
            LastImportDate = lastImport?.CreatedAt,
            SuggestedDeadline = suggestedDeadline,
            StudentCount = studentCount,
        });
    }
}
