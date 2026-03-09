using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

public class GetStudentByIdQueryHandler : IGetStudentByIdQueryHandler
{
    private readonly IApplicationDbContext _db;
    public GetStudentByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<StudentDetailDto>> HandleAsync(GetStudentByIdQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null || user.SchoolID == null)
            return Result<StudentDetailDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolId = user.SchoolID.Value;

        var child = await _db.ChildProfiles
            .AsNoTracking()
            .Where(c => c.Id == query.StudentId && c.SchoolID == schoolId && !c.IsDeleted)
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
                ImportPhone = c.StudentDataImports
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.ParentPhone)
                    .FirstOrDefault(),
                ImportCode = c.StudentDataImports
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.StudentCode)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        if (child == null)
            return Result<StudentDetailDto>.Failure("Student not found.", "STUDENT_NOT_FOUND");

        return Result<StudentDetailDto>.Success(new StudentDetailDto
        {
            Id = child.Id,
            FullName = child.FullName,
            StudentCode = child.ImportCode,
            Grade = child.Grade,
            Gender = child.Gender.ToString(),
            DateOfBirth = child.DOB,
            HeightCm = child.HeightCm,
            WeightKg = child.WeightKg,
            HasMeasurements = child.HeightCm > 0 && child.WeightKg > 0,
            ParentName = child.ParentFullName,
            ParentPhone = child.ParentPhone ?? child.ImportPhone,
            IsParentLinked = child.ParentUserID != null,
        });
    }
}
