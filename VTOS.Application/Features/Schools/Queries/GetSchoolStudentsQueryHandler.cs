using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

public class GetSchoolStudentsQueryHandler : IGetSchoolStudentsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolStudentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<StudentListResponse>> HandleAsync(GetSchoolStudentsQuery query, CancellationToken ct = default)
    {
        // 1. Resolve school from user
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null)
            return Result<StudentListResponse>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var schoolId = schoolMgr.SchoolID;

        // 2. Base query: ChildProfiles for this school
        var q = _db.ChildProfiles
            .AsNoTracking()
            .Where(c => c.SchoolID == schoolId && !c.IsDeleted);

        // 3. Apply search filter (name or grade)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(c =>
                c.FullName.ToLower().Contains(search) ||
                c.Grade.ToLower().Contains(search));
        }

        // 4. Grade filter
        if (!string.IsNullOrWhiteSpace(query.Grade))
            q = q.Where(c => c.Grade == query.Grade);

        // 5. Measurement status filter (has height+weight > 0)
        if (!string.IsNullOrWhiteSpace(query.MeasurementStatus))
        {
            if (query.MeasurementStatus == "updated")
                q = q.Where(c => c.HeightCm > 0 && c.WeightKg > 0);
            else if (query.MeasurementStatus == "missing")
                q = q.Where(c => c.HeightCm == 0 || c.WeightKg == 0);
        }

        // 6. Parent link status filter
        if (!string.IsNullOrWhiteSpace(query.ParentLinkStatus))
        {
            if (query.ParentLinkStatus == "linked")
                q = q.Where(c => c.ParentUserID != null);
            else if (query.ParentLinkStatus == "unlinked")
                q = q.Where(c => c.ParentUserID == null);
        }

        // 7. Total count
        var totalCount = await q.CountAsync(ct);

        // 8. Paginate & join to get extra data
        var children = await q
            .OrderBy(c => c.FullName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new
            {
                c.Id,
                c.FullName,
                c.Grade,
                c.Gender,
                c.DOB,
                c.HeightCm,
                c.WeightKg,
                c.ParentUserID,
                ParentFullName = c.ParentUserID != null ? c.ParentUser.FullName : null,
                ParentPhone = c.ParentUserID != null ? c.ParentUser.Phone : null,
                // Also try to get phone from StudentDataImport if parent not linked
                ImportPhone = c.StudentDataImports
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.ParentPhone)
                    .FirstOrDefault(),
                ImportCode = c.StudentDataImports
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.StudentCode)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = children.Select(c => new StudentListItemDto
        {
            Id = c.Id,
            FullName = c.FullName,
            StudentCode = c.ImportCode,
            Grade = c.Grade,
            Gender = c.Gender.ToString(),
            DateOfBirth = c.DOB,
            HasMeasurements = c.HeightCm > 0 && c.WeightKg > 0,
            ParentName = c.ParentFullName,
            ParentPhone = c.ParentPhone ?? c.ImportPhone,
            IsParentLinked = c.ParentUserID != null,
        }).ToList();

        return Result<StudentListResponse>.Success(new StudentListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        });
    }
}
