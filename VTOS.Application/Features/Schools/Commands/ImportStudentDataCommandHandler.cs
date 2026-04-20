using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-43: Import student data from .xlsx.
/// Session 8 extends the flow with optional homeroom teacher provisioning and class-group creation.
/// </summary>
public class ImportStudentDataCommandHandler : IImportStudentDataCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public ImportStudentDataCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<Result<ImportStudentResultDto>> HandleAsync(ImportStudentDataCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<ImportStudentResultDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolMgr = await _db.SchoolManagers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<ImportStudentResultDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolId = schoolMgr.SchoolID;
        var academicYear = GetCurrentAcademicYear();

        var teacherRole = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleName == "HomeroomTeacher", ct);

        if (teacherRole == null)
            return Result<ImportStudentResultDto>.Failure("HomeroomTeacher role is missing.", "ROLE_NOT_FOUND");

        var existingChildren = await _db.ChildProfiles
            .AsNoTracking()
            .Where(c => c.SchoolID == schoolId && !c.IsDeleted)
            .Select(c => new { c.FullName, c.DOB })
            .ToListAsync(ct);

        var existingSet = existingChildren
            .Select(c => MakeKey(c.FullName, c.DOB))
            .ToHashSet();

        var teacherEmails = command.Rows
            .Where(r => r.Length >= 7 && !string.IsNullOrWhiteSpace(r[6]))
            .Select(r => NormalizeEmail(r[6]))
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .ToList();

        var teacherUsersByEmail = await _db.Users
            .Include(u => u.Role)
            .Where(u => teacherEmails.Contains(u.Email.ToLower()))
            .ToDictionaryAsync(u => u.Email.ToLower(), ct);

        var classNames = command.Rows
            .Where(r => r.Length >= 3 && !string.IsNullOrWhiteSpace(r[2]))
            .Select(r => NormalizeClassName(r[2]))
            .Distinct()
            .ToList();

        var classGroupsByKey = await _db.ClassGroups
            .Where(cg => cg.SchoolID == schoolId && cg.AcademicYear == academicYear && classNames.Contains(cg.ClassName.ToUpper()))
            .ToDictionaryAsync(cg => MakeClassKey(cg.ClassName, cg.AcademicYear), ct);

        var result = new ImportStudentResultDto();
        var newChildren = new List<ChildProfile>();
        var newImports = new List<StudentDataImport>();
        var newTeacherUsers = new List<User>();
        var newClassGroups = new List<ClassGroup>();
        var pendingTeacherEmails = new List<(string Email, string TempPassword)>();
        var errors = new List<ImportErrorDto>();

        int rowNumber = 1;
        foreach (var columns in command.Rows)
        {
            rowNumber++;
            result.TotalRows++;

            try
            {
                if (columns.Length < 5)
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        StudentName = columns.Length > 0 ? columns[0] : null,
                        ErrorMessage = $"Expected at least 5 columns, found {columns.Length}."
                    });
                    result.ErrorCount++;
                    continue;
                }

                var fullName = columns[0].Trim();
                var dobStr = columns[1].Trim();
                var className = columns[2].Trim();
                var gender = columns[3].Trim();
                var parentPhone = columns[4].Trim();
                var homeroomTeacherName = columns.Length >= 6 ? columns[5].Trim() : string.Empty;
                var homeroomTeacherEmail = columns.Length >= 7 ? NormalizeEmail(columns[6]) : string.Empty;

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    errors.Add(new ImportErrorDto { RowNumber = rowNumber, ErrorMessage = "Student Name is required." });
                    result.ErrorCount++;
                    continue;
                }

                if (fullName.Length > 255)
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        StudentName = fullName[..50],
                        ErrorMessage = "Student Name exceeds 255 characters."
                    });
                    result.ErrorCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(className))
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        StudentName = fullName,
                        ErrorMessage = "Class is required."
                    });
                    result.ErrorCount++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(homeroomTeacherName) ^ !string.IsNullOrWhiteSpace(homeroomTeacherEmail))
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        StudentName = fullName,
                        ErrorMessage = "Homeroom Teacher Name and Email must be provided together."
                    });
                    result.ErrorCount++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(homeroomTeacherEmail) &&
                    !IsValidEmail(homeroomTeacherEmail))
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        StudentName = fullName,
                        ErrorMessage = $"Invalid homeroom teacher email '{columns[6].Trim()}'."
                    });
                    result.ErrorCount++;
                    continue;
                }

                DateTime? dob = null;
                if (!string.IsNullOrWhiteSpace(dobStr))
                {
                    if (DateTime.TryParseExact(
                        dobStr,
                        new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedDob))
                    {
                        dob = DateTime.SpecifyKind(parsedDob, DateTimeKind.Utc);
                    }
                    else
                    {
                        errors.Add(new ImportErrorDto
                        {
                            RowNumber = rowNumber,
                            StudentName = fullName,
                            ErrorMessage = $"Invalid DOB format '{dobStr}'. Expected dd/MM/yyyy."
                        });
                        result.ErrorCount++;
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(parentPhone))
                {
                    parentPhone = parentPhone.Replace(" ", "").Replace("-", "");
                    if (parentPhone.Length < 10 || parentPhone.Length > 11 || !parentPhone.All(char.IsDigit))
                    {
                        errors.Add(new ImportErrorDto
                        {
                            RowNumber = rowNumber,
                            StudentName = fullName,
                            ErrorMessage = $"Invalid phone number '{columns[4].Trim()}'. Expected 10-11 digits."
                        });
                        result.ErrorCount++;
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(gender) && gender.Length > 20)
                    gender = gender[..20];

                var key = MakeKey(fullName, dob);
                if (existingSet.Contains(key))
                {
                    result.SkippedCount++;
                    continue;
                }

                Guid? homeroomTeacherId = null;
                if (!string.IsNullOrWhiteSpace(homeroomTeacherEmail))
                {
                    if (teacherUsersByEmail.TryGetValue(homeroomTeacherEmail, out var existingTeacher))
                    {
                        if (!string.Equals(existingTeacher.Role.RoleName, "HomeroomTeacher", StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add(new ImportErrorDto
                            {
                                RowNumber = rowNumber,
                                StudentName = fullName,
                                ErrorMessage = $"Email '{columns[6].Trim()}' is already used by another role."
                            });
                            result.ErrorCount++;
                            continue;
                        }

                        homeroomTeacherId = existingTeacher.Id;
                    }
                    else
                    {
                        var tempPassword = GenerateTempPassword();
                        var teacher = new User
                        {
                            Id = Guid.NewGuid(),
                            FullName = homeroomTeacherName,
                            Email = homeroomTeacherEmail,
                            PasswordHash = _passwordHasher.HashPassword(tempPassword),
                            Phone = null,
                            Avatar = string.Empty,
                            RoleID = teacherRole.Id,
                            IsActive = true,
                            IsDeleted = false,
                            AuthProvider = "Local",
                            CreatedAt = DateTime.UtcNow
                        };
                        teacher.Role = teacherRole;

                        newTeacherUsers.Add(teacher);
                        teacherUsersByEmail[homeroomTeacherEmail] = teacher;
                        pendingTeacherEmails.Add((teacher.Email, tempPassword));
                        homeroomTeacherId = teacher.Id;
                    }
                }

                var normalizedClassName = NormalizeClassName(className);
                var classKey = MakeClassKey(normalizedClassName, academicYear);
                if (!classGroupsByKey.TryGetValue(classKey, out var classGroup))
                {
                    classGroup = new ClassGroup
                    {
                        Id = Guid.NewGuid(),
                        SchoolID = schoolId,
                        ClassName = normalizedClassName,
                        Grade = ExtractGrade(normalizedClassName),
                        AcademicYear = academicYear,
                        HomeroomTeacherID = homeroomTeacherId,
                        CreatedAt = DateTime.UtcNow
                    };

                    newClassGroups.Add(classGroup);
                    classGroupsByKey[classKey] = classGroup;
                }
                else if (homeroomTeacherId.HasValue)
                {
                    if (classGroup.HomeroomTeacherID.HasValue && classGroup.HomeroomTeacherID != homeroomTeacherId)
                    {
                        errors.Add(new ImportErrorDto
                        {
                            RowNumber = rowNumber,
                            StudentName = fullName,
                            ErrorMessage = $"Class '{normalizedClassName}' already maps to a different homeroom teacher."
                        });
                        result.ErrorCount++;
                        continue;
                    }

                    classGroup.HomeroomTeacherID = homeroomTeacherId;
                }

                int age = dob.HasValue
                    ? DateTime.UtcNow.Year - dob.Value.Year - (DateTime.UtcNow.DayOfYear < dob.Value.DayOfYear ? 1 : 0)
                    : 0;

                var genderEnum = ParseGender(gender);
                var childId = Guid.NewGuid();
                newChildren.Add(new ChildProfile
                {
                    Id = childId,
                    SchoolID = schoolId,
                    ClassGroupID = classGroup.Id,
                    ParentUserID = null,
                    FullName = fullName,
                    DOB = dob,
                    Age = age,
                    Grade = normalizedClassName,
                    Gender = genderEnum,
                    Avatar = string.Empty,
                    IsDeleted = false,
                    ParentPhone = string.IsNullOrWhiteSpace(parentPhone) ? null : parentPhone,
                });

                newImports.Add(new StudentDataImport
                {
                    Id = Guid.NewGuid(),
                    SchoolID = schoolId,
                    FullName = fullName,
                    DateOfBirth = dob,
                    Class = normalizedClassName,
                    Gender = string.IsNullOrWhiteSpace(gender) ? null : gender,
                    ParentPhone = string.IsNullOrWhiteSpace(parentPhone) ? null : parentPhone,
                    HomeroomTeacherName = string.IsNullOrWhiteSpace(homeroomTeacherName) ? null : homeroomTeacherName,
                    HomeroomTeacherEmail = string.IsNullOrWhiteSpace(homeroomTeacherEmail) ? null : homeroomTeacherEmail,
                    IsRegistered = false,
                    MatchedChildID = childId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });

                existingSet.Add(key);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportErrorDto
                {
                    RowNumber = rowNumber,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                });
                result.ErrorCount++;
            }
        }

        if (newTeacherUsers.Count > 0)
            _db.Users.AddRange(newTeacherUsers);

        if (newClassGroups.Count > 0)
            _db.ClassGroups.AddRange(newClassGroups);

        if (newChildren.Count > 0)
        {
            _db.ChildProfiles.AddRange(newChildren);
            _db.StudentDataImports.AddRange(newImports);
        }

        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            SchoolID = schoolId,
            FileName = command.FileName,
            TotalRows = result.TotalRows,
            SuccessCount = result.SuccessCount,
            SkippedCount = result.SkippedCount,
            ErrorCount = result.ErrorCount,
            CreatedAt = DateTime.UtcNow,
        };
        _db.ImportBatches.Add(batch);

        await _db.SaveChangesAsync(ct);

        foreach (var (email, tempPassword) in pendingTeacherEmails)
        {
            try
            {
                await _emailService.SendAccountCredentialsEmailAsync(email, tempPassword, "HomeroomTeacher", ct);
            }
            catch
            {
                // Import should not fail if email delivery is unavailable.
            }
        }

        result.Errors = errors;
        return Result<ImportStudentResultDto>.Success(result);
    }

    private static string MakeKey(string? name, DateTime? dob)
        => $"{name?.Trim().ToLowerInvariant()}|{dob:yyyy-MM-dd}";

    private static string MakeClassKey(string className, string academicYear)
        => $"{NormalizeClassName(className)}|{academicYear}";

    private static string NormalizeClassName(string raw)
        => raw.Trim().ToUpperInvariant();

    private static string NormalizeEmail(string raw)
        => raw.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
        => email.Contains('@') && email.Contains('.');

    private static string ExtractGrade(string className)
    {
        var digits = new string(className.Trim().TakeWhile(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? className.Trim() : digits;
    }

    private static string GetCurrentAcademicYear()
    {
        var now = DateTime.UtcNow;
        return now.Month >= 8
            ? $"{now.Year}-{now.Year + 1}"
            : $"{now.Year - 1}-{now.Year}";
    }

    private static string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        Span<char> buffer = stackalloc char[10];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }

        return new string(buffer);
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
