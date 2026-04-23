using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

public class GetSchoolClassesOverviewQueryHandler : IGetSchoolClassesOverviewQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolClassesOverviewQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SchoolClassesOverviewDto>> HandleAsync(GetSchoolClassesOverviewQuery query, CancellationToken ct = default)
    {
        var schoolId = await _db.SchoolManagers
            .AsNoTracking()
            .Where(sm => sm.UserID == query.UserId)
            .Select(sm => (Guid?)sm.SchoolID)
            .FirstOrDefaultAsync(ct);

        if (!schoolId.HasValue)
            return Result<SchoolClassesOverviewDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var academicYear = query.AcademicYear;
        if (string.IsNullOrWhiteSpace(academicYear))
        {
            academicYear = await _db.ClassGroups
                .AsNoTracking()
                .Where(cg => cg.SchoolID == schoolId.Value)
                .OrderByDescending(cg => cg.AcademicYear)
                .Select(cg => cg.AcademicYear)
                .FirstOrDefaultAsync(ct)
                ?? string.Empty;
        }

        var classGroups = await _db.ClassGroups
            .AsNoTracking()
            .Where(cg => cg.SchoolID == schoolId.Value && (string.IsNullOrWhiteSpace(academicYear) || cg.AcademicYear == academicYear))
            .Select(cg => new ClassGroupSummaryDto
            {
                Id = cg.Id,
                ClassName = cg.ClassName,
                Grade = cg.Grade,
                AcademicYear = cg.AcademicYear,
                HomeroomTeacherName = cg.HomeroomTeacher != null ? cg.HomeroomTeacher.FullName : null,
                HomeroomTeacherEmail = cg.HomeroomTeacher != null ? cg.HomeroomTeacher.Email : null,
                StudentCount = cg.Students.Count(s => !s.IsDeleted),
                MeasurementReadyCount = cg.Students.Count(s => !s.IsDeleted && s.HeightCm > 0 && s.WeightKg > 0),
                ParentLinkedCount = cg.Students.Count(s => !s.IsDeleted && s.ParentUserID != null),
                OrderedStudentCount = cg.Students.Count(s => !s.IsDeleted && s.Orders.Any(o => o.OrderStatus != Domain.Enums.OrderStatus.Cancelled && o.OrderStatus != Domain.Enums.OrderStatus.Refunded)),
            })
            .OrderBy(cg => cg.Grade)
            .ThenBy(cg => cg.ClassName)
            .ToListAsync(ct);

        var unassignedStudentCount = await _db.ChildProfiles
            .AsNoTracking()
            .Where(c => c.SchoolID == schoolId.Value && !c.IsDeleted && c.ClassGroupID == null)
            .CountAsync(ct);

        var grades = classGroups
            .GroupBy(cg => cg.Grade)
            .OrderBy(g => g.Key)
            .Select(g => new GradeClassGroupDto
            {
                Grade = g.Key,
                ClassCount = g.Count(),
                StudentCount = g.Sum(x => x.StudentCount),
                Classes = g.ToList(),
            })
            .ToList();

        return Result<SchoolClassesOverviewDto>.Success(new SchoolClassesOverviewDto
        {
            AcademicYear = academicYear ?? string.Empty,
            TotalClasses = classGroups.Count,
            TotalStudents = classGroups.Sum(cg => cg.StudentCount),
            UnassignedStudentCount = unassignedStudentCount,
            Grades = grades,
        });
    }
}

public class GetSchoolClassDetailQueryHandler : IGetSchoolClassDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolClassDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ClassGroupDetailDto>> HandleAsync(GetSchoolClassDetailQuery query, CancellationToken ct = default)
    {
        var schoolId = await _db.SchoolManagers
            .AsNoTracking()
            .Where(sm => sm.UserID == query.UserId)
            .Select(sm => (Guid?)sm.SchoolID)
            .FirstOrDefaultAsync(ct);

        if (!schoolId.HasValue)
            return Result<ClassGroupDetailDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var detail = await BuildClassDetailQuery()
            .FirstOrDefaultAsync(cg => cg.Id == query.ClassGroupId && cg.SchoolID == schoolId.Value, ct);

        if (detail == null)
            return Result<ClassGroupDetailDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        return Result<ClassGroupDetailDto>.Success(detail);
    }

    private IQueryable<ClassGroupDetailDto> BuildClassDetailQuery()
    {
        return _db.ClassGroups
            .AsNoTracking()
            .Select(cg => new ClassGroupDetailDto
            {
                Id = cg.Id,
                SchoolID = cg.SchoolID,
                SchoolName = cg.School.SchoolName,
                ClassName = cg.ClassName,
                Grade = cg.Grade,
                AcademicYear = cg.AcademicYear,
                HomeroomTeacher = cg.HomeroomTeacherID == null
                    ? null
                    : new ClassTeacherDto
                    {
                        Id = cg.HomeroomTeacher!.Id,
                        FullName = cg.HomeroomTeacher.FullName,
                        Email = cg.HomeroomTeacher.Email,
                    },
                StudentCount = cg.Students.Count(s => !s.IsDeleted),
                MeasurementReadyCount = cg.Students.Count(s => !s.IsDeleted && s.HeightCm > 0 && s.WeightKg > 0),
                ParentLinkedCount = cg.Students.Count(s => !s.IsDeleted && s.ParentUserID != null),
                Students = cg.Students
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.FullName)
                    .Select(s => new ClassStudentItemDto
                    {
                        Id = s.Id,
                        FullName = s.FullName,
                        StudentCode = s.StudentDataImports
                            .OrderByDescending(i => i.CreatedAt)
                            .Select(i => i.StudentCode)
                            .FirstOrDefault(),
                        Grade = s.Grade,
                        Gender = s.Gender.ToString(),
                        DateOfBirth = s.DOB,
                        HasMeasurements = s.HeightCm > 0 && s.WeightKg > 0,
                        ParentName = s.ParentUserID != null ? s.ParentUser.FullName : null,
                        ParentPhone = s.ParentUserID != null
                            ? s.ParentUser.Phone
                            : s.StudentDataImports
                                .OrderByDescending(i => i.CreatedAt)
                                .Select(i => i.ParentPhone)
                                .FirstOrDefault(),
                        IsParentLinked = s.ParentUserID != null,
                    })
                    .ToList(),
            });
    }
}

public class GetTeacherClassesOverviewQueryHandler : IGetTeacherClassesOverviewQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherClassesOverviewQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherClassesOverviewDto>> HandleAsync(GetTeacherClassesOverviewQuery query, CancellationToken ct = default)
    {
        var teacher = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == query.UserId && !u.IsDeleted, ct);

        if (teacher == null || !string.Equals(teacher.Role?.RoleName, "HomeroomTeacher", StringComparison.OrdinalIgnoreCase))
            return Result<TeacherClassesOverviewDto>.Failure("Teacher account not found.", "TEACHER_NOT_FOUND");

        var classes = await _db.ClassGroups
            .AsNoTracking()
            .Where(cg => cg.HomeroomTeacherID == query.UserId)
            .Select(cg => new ClassGroupSummaryDto
            {
                Id = cg.Id,
                ClassName = cg.ClassName,
                Grade = cg.Grade,
                AcademicYear = cg.AcademicYear,
                HomeroomTeacherName = teacher.FullName,
                HomeroomTeacherEmail = teacher.Email,
                StudentCount = cg.Students.Count(s => !s.IsDeleted),
                MeasurementReadyCount = cg.Students.Count(s => !s.IsDeleted && s.HeightCm > 0 && s.WeightKg > 0),
                ParentLinkedCount = cg.Students.Count(s => !s.IsDeleted && s.ParentUserID != null),
                OrderedStudentCount = cg.Students.Count(s => !s.IsDeleted && s.Orders.Any(o => o.OrderStatus != Domain.Enums.OrderStatus.Cancelled && o.OrderStatus != Domain.Enums.OrderStatus.Refunded)),
            })
            .OrderBy(cg => cg.AcademicYear)
            .ThenBy(cg => cg.ClassName)
            .ToListAsync(ct);

        return Result<TeacherClassesOverviewDto>.Success(new TeacherClassesOverviewDto
        {
            TeacherId = teacher.Id,
            TeacherName = teacher.FullName,
            TeacherEmail = teacher.Email,
            TotalClasses = classes.Count,
            TotalStudents = classes.Sum(c => c.StudentCount),
            Classes = classes,
        });
    }
}

public class GetTeacherClassDetailQueryHandler : IGetTeacherClassDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherClassDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ClassGroupDetailDto>> HandleAsync(GetTeacherClassDetailQuery query, CancellationToken ct = default)
    {
        var teacherExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == query.UserId && !u.IsDeleted, ct);

        if (!teacherExists)
            return Result<ClassGroupDetailDto>.Failure("Teacher account not found.", "TEACHER_NOT_FOUND");

        var detail = await _db.ClassGroups
            .AsNoTracking()
            .Where(cg => cg.HomeroomTeacherID == query.UserId && cg.Id == query.ClassGroupId)
            .Select(cg => new ClassGroupDetailDto
            {
                Id = cg.Id,
                SchoolID = cg.SchoolID,
                SchoolName = cg.School.SchoolName,
                ClassName = cg.ClassName,
                Grade = cg.Grade,
                AcademicYear = cg.AcademicYear,
                HomeroomTeacher = cg.HomeroomTeacherID == null
                    ? null
                    : new ClassTeacherDto
                    {
                        Id = cg.HomeroomTeacher!.Id,
                        FullName = cg.HomeroomTeacher.FullName,
                        Email = cg.HomeroomTeacher.Email,
                    },
                StudentCount = cg.Students.Count(s => !s.IsDeleted),
                MeasurementReadyCount = cg.Students.Count(s => !s.IsDeleted && s.HeightCm > 0 && s.WeightKg > 0),
                ParentLinkedCount = cg.Students.Count(s => !s.IsDeleted && s.ParentUserID != null),
                Students = cg.Students
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.FullName)
                    .Select(s => new ClassStudentItemDto
                    {
                        Id = s.Id,
                        FullName = s.FullName,
                        StudentCode = s.StudentDataImports
                            .OrderByDescending(i => i.CreatedAt)
                            .Select(i => i.StudentCode)
                            .FirstOrDefault(),
                        Grade = s.Grade,
                        Gender = s.Gender.ToString(),
                        DateOfBirth = s.DOB,
                        HasMeasurements = s.HeightCm > 0 && s.WeightKg > 0,
                        ParentName = s.ParentUserID != null ? s.ParentUser.FullName : null,
                        ParentPhone = s.ParentUserID != null
                            ? s.ParentUser.Phone
                            : s.StudentDataImports
                                .OrderByDescending(i => i.CreatedAt)
                                .Select(i => i.ParentPhone)
                                .FirstOrDefault(),
                        IsParentLinked = s.ParentUserID != null,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (detail == null)
            return Result<ClassGroupDetailDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        return Result<ClassGroupDetailDto>.Success(detail);
    }
}
