using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public class GetSchoolGradesQueryHandler : IGetSchoolGradesQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolGradesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<string>>> HandleAsync(GetSchoolGradesQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null || user.SchoolID == null)
            return Result<IReadOnlyList<string>>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolId = user.SchoolID.Value;

        var grades = await _db.ChildProfiles
            .AsNoTracking()
            .Where(c => c.SchoolID == schoolId && !c.IsDeleted && !string.IsNullOrEmpty(c.Grade))
            .Select(c => c.Grade)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(ct);

        return Result<IReadOnlyList<string>>.Success(grades);
    }
}
