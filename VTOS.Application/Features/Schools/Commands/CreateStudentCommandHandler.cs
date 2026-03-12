using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

public class CreateStudentCommandHandler : ICreateStudentCommandHandler
{
    private readonly IApplicationDbContext _db;
    public CreateStudentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand cmd, CancellationToken ct = default)
    {
        // 1. Resolve school
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct);
        if (user == null || user.SchoolID == null)
            return Result<StudentDetailDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolId = user.SchoolID.Value;

        // 2. Validate
        if (string.IsNullOrWhiteSpace(cmd.FullName))
            return Result<StudentDetailDto>.Failure("Student name is required.", "NAME_REQUIRED");

        // 3. Duplicate check
        var key = $"{cmd.FullName.Trim().ToLowerInvariant()}|{cmd.DateOfBirth:yyyy-MM-dd}";
        var exists = await _db.ChildProfiles.AsNoTracking()
            .Where(c => c.SchoolID == schoolId && !c.IsDeleted)
            .AnyAsync(c => (c.FullName.ToLower() == cmd.FullName.Trim().ToLower()) && c.DOB == cmd.DateOfBirth, ct);
        if (exists)
            return Result<StudentDetailDto>.Failure("A student with this name and DOB already exists.", "DUPLICATE");

        // 4. Parse gender
        var genderEnum = ParseGender(cmd.Gender);

        // 5. Calculate age
        int age = cmd.DateOfBirth.HasValue
            ? DateTime.UtcNow.Year - cmd.DateOfBirth.Value.Year - (DateTime.UtcNow.DayOfYear < cmd.DateOfBirth.Value.DayOfYear ? 1 : 0)
            : 0;

        // 6. Create ChildProfile
        var childId = Guid.NewGuid();
        var child = new ChildProfile
        {
            Id = childId,
            SchoolID = schoolId,
            ParentUserID = null,
            FullName = cmd.FullName.Trim(),
            DOB = cmd.DateOfBirth.HasValue ? DateTime.SpecifyKind(cmd.DateOfBirth.Value, DateTimeKind.Utc) : null,
            Age = age,
            Grade = cmd.Grade?.Trim() ?? string.Empty,
            Gender = genderEnum,
            Avatar = string.Empty,
            HeightCm = cmd.HeightCm ?? 0,
            WeightKg = cmd.WeightKg ?? 0,
            IsDeleted = false,
            ParentPhone = string.IsNullOrWhiteSpace(cmd.ParentPhone) ? null : cmd.ParentPhone.Trim(),
        };
        _db.ChildProfiles.Add(child);

        // 7. Create StudentDataImport log
        _db.StudentDataImports.Add(new StudentDataImport
        {
            Id = Guid.NewGuid(),
            SchoolID = schoolId,
            FullName = child.FullName,
            DateOfBirth = child.DOB,
            Class = string.IsNullOrWhiteSpace(cmd.Grade) ? null : cmd.Grade.Trim(),
            Gender = cmd.Gender,
            ParentPhone = cmd.ParentPhone,
            IsRegistered = false,
            MatchedChildID = childId,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);

        return Result<StudentDetailDto>.Success(new StudentDetailDto
        {
            Id = childId,
            FullName = child.FullName,
            Grade = child.Grade,
            Gender = child.Gender.ToString(),
            DateOfBirth = child.DOB,
            HeightCm = child.HeightCm,
            WeightKg = child.WeightKg,
            HasMeasurements = child.HeightCm > 0 && child.WeightKg > 0,
            ParentPhone = cmd.ParentPhone,
            IsParentLinked = false,
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
