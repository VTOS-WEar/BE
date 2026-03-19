using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

public class UpdateStudentCommandHandler : IUpdateStudentCommandHandler
{
    private readonly IApplicationDbContext _db;
    public UpdateStudentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<StudentDetailDto>> HandleAsync(UpdateStudentCommand cmd, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct);
        if (user == null)
            return Result<StudentDetailDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var schoolId = schoolMgr.SchoolID;

        var child = await _db.ChildProfiles
            .Include(c => c.School)
            .FirstOrDefaultAsync(c => c.Id == cmd.StudentId && c.SchoolID == schoolId && !c.IsDeleted, ct);

        if (child == null)
            return Result<StudentDetailDto>.Failure("Student not found.", "STUDENT_NOT_FOUND");

        // Apply updates
        if (!string.IsNullOrWhiteSpace(cmd.FullName))
            child.FullName = cmd.FullName.Trim();

        if (cmd.DateOfBirth.HasValue)
        {
            child.DOB = DateTime.SpecifyKind(cmd.DateOfBirth.Value, DateTimeKind.Utc);
            child.Age = DateTime.UtcNow.Year - child.DOB.Value.Year -
                (DateTime.UtcNow.DayOfYear < child.DOB.Value.DayOfYear ? 1 : 0);
        }

        if (!string.IsNullOrWhiteSpace(cmd.Grade))
            child.Grade = cmd.Grade.Trim();

        if (!string.IsNullOrWhiteSpace(cmd.Gender))
            child.Gender = ParseGender(cmd.Gender);

        if (cmd.HeightCm.HasValue)
            child.HeightCm = cmd.HeightCm.Value;

        if (cmd.WeightKg.HasValue)
            child.WeightKg = cmd.WeightKg.Value;

        // Update ParentPhone only if child is NOT yet linked to a real parent account
        if (!string.IsNullOrWhiteSpace(cmd.ParentPhone) && child.ParentUserID == null)
            child.ParentPhone = cmd.ParentPhone.Trim();

        await _db.SaveChangesAsync(ct);

        return Result<StudentDetailDto>.Success(new StudentDetailDto
        {
            Id = child.Id,
            FullName = child.FullName,
            Grade = child.Grade,
            Gender = child.Gender.ToString(),
            DateOfBirth = child.DOB,
            HeightCm = child.HeightCm,
            WeightKg = child.WeightKg,
            HasMeasurements = child.HeightCm > 0 && child.WeightKg > 0,
            IsParentLinked = child.ParentUserID != null,
            ParentPhone = child.ParentPhone,
        });
    }

    private static VTOS.Domain.Enums.Gender ParseGender(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return VTOS.Domain.Enums.Gender.Other;
        return raw.Trim().ToLowerInvariant() switch
        {
            "nam" or "male" or "m" => VTOS.Domain.Enums.Gender.Male,
            "nữ" or "nu" or "female" or "f" => VTOS.Domain.Enums.Gender.Female,
            _ => VTOS.Domain.Enums.Gender.Other
        };
    }
}
