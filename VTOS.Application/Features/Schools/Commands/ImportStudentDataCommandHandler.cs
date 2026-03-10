using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-43: Import student data from .xlsx or .csv.
/// Design:
///   1. Creates a Children record (ParentUserID = null — not linked to a parent yet)
///   2. Creates a StudentDataImport log record with MatchedChildID pointing to the Children row
/// Duplicate detection: load school's Children → HashSet(FullName+DOB) → O(1) per row
/// </summary>
public class ImportStudentDataCommandHandler : IImportStudentDataCommandHandler
{
    private readonly IApplicationDbContext _db;

    public ImportStudentDataCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ImportStudentResultDto>> HandleAsync(ImportStudentDataCommand command, CancellationToken ct = default)
    {
        // 1. Resolve school
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null || user.SchoolID == null)
            return Result<ImportStudentResultDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolId = user.SchoolID.Value;

        // 2. Load existing Children for this school → HashSet for O(1) duplicate check
        var existingChildren = await _db.ChildProfiles
            .AsNoTracking()
            .Where(c => c.SchoolID == schoolId && !c.IsDeleted)
            .Select(c => new { c.FullName, c.DOB })
            .ToListAsync(ct);

        var existingSet = existingChildren
            .Select(c => MakeKey(c.FullName, c.DOB))
            .ToHashSet();

        // 3. Process rows
        var result = new ImportStudentResultDto();
        var newChildren   = new List<ChildProfile>();
        var newImports    = new List<StudentDataImport>();
        var errors        = new List<ImportErrorDto>();

        int rowNumber = 1; // row 1 = header (excluded by caller)
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
                        ErrorMessage = $"Expected 5 columns, found {columns.Length}."
                    });
                    result.ErrorCount++;
                    continue;
                }

                var fullName    = columns[0].Trim();
                var dobStr      = columns[1].Trim();
                var grade       = columns[2].Trim();
                var gender      = columns[3].Trim();
                var parentPhone = columns[4].Trim();

                // Validate FullName
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    errors.Add(new ImportErrorDto { RowNumber = rowNumber, ErrorMessage = "Student Name is required." });
                    result.ErrorCount++;
                    continue;
                }
                if (fullName.Length > 255)
                {
                    errors.Add(new ImportErrorDto { RowNumber = rowNumber, StudentName = fullName[..50], ErrorMessage = "Student Name exceeds 255 characters." });
                    result.ErrorCount++;
                    continue;
                }

                // Parse DOB
                DateTime? dob = null;
                if (!string.IsNullOrWhiteSpace(dobStr))
                {
                    if (DateTime.TryParseExact(dobStr, new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDob))
                    {
                        dob = DateTime.SpecifyKind(parsedDob, DateTimeKind.Utc);
                    }
                    else
                    {
                        errors.Add(new ImportErrorDto { RowNumber = rowNumber, StudentName = fullName, ErrorMessage = $"Invalid DOB format '{dobStr}'. Expected dd/MM/yyyy." });
                        result.ErrorCount++;
                        continue;
                    }
                }

                // Validate phone
                if (!string.IsNullOrWhiteSpace(parentPhone))
                {
                    parentPhone = parentPhone.Replace(" ", "").Replace("-", "");
                    if (parentPhone.Length < 10 || parentPhone.Length > 11 || !parentPhone.All(char.IsDigit))
                    {
                        errors.Add(new ImportErrorDto { RowNumber = rowNumber, StudentName = fullName, ErrorMessage = $"Invalid phone number '{columns[4].Trim()}'. Expected 10-11 digits." });
                        result.ErrorCount++;
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(gender) && gender.Length > 20)
                    gender = gender[..20];

                // Duplicate check (existing DB + current batch)
                var key = MakeKey(fullName, dob);
                if (existingSet.Contains(key)) { result.SkippedCount++; continue; }
                if (newChildren.Any(c => MakeKey(c.FullName, c.DOB) == key)) { result.SkippedCount++; continue; }

                // Calculate age from DOB
                int age = dob.HasValue
                    ? DateTime.UtcNow.Year - dob.Value.Year - (DateTime.UtcNow.DayOfYear < dob.Value.DayOfYear ? 1 : 0)
                    : 0;

                // Parse gender to enum (default Unknown)
                var genderEnum = ParseGender(gender);

                // --- Create Children record ---
                var childId = Guid.NewGuid();
                var child = new ChildProfile
                {
                    Id           = childId,
                    SchoolID     = schoolId,
                    ParentUserID = null,          // will be linked when Parent registers
                    FullName     = fullName,
                    DOB          = dob,
                    Age          = age,
                    Grade        = string.IsNullOrWhiteSpace(grade) ? string.Empty : grade,
                    Gender       = genderEnum,
                    Avatar       = string.Empty,
                    IsDeleted    = false
                };
                newChildren.Add(child);

                // --- Create StudentDataImport log record ---
                newImports.Add(new StudentDataImport
                {
                    Id             = Guid.NewGuid(),
                    SchoolID       = schoolId,
                    FullName       = fullName,
                    DateOfBirth    = dob,
                    Class          = string.IsNullOrWhiteSpace(grade) ? null : grade,
                    Gender         = string.IsNullOrWhiteSpace(gender) ? null : gender,
                    ParentPhone    = string.IsNullOrWhiteSpace(parentPhone) ? null : parentPhone,
                    IsRegistered   = false,
                    MatchedChildID = childId,
                    CreatedAt      = DateTime.UtcNow
                });

                existingSet.Add(key);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportErrorDto { RowNumber = rowNumber, ErrorMessage = $"Unexpected error: {ex.Message}" });
                result.ErrorCount++;
            }
        }

        // 4. Batch insert both tables atomically
        if (newChildren.Count > 0)
        {
            _db.ChildProfiles.AddRange(newChildren);
            _db.StudentDataImports.AddRange(newImports);
        }

        // 5. Create ImportBatch record to track this import session
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

        result.Errors = errors;
        return Result<ImportStudentResultDto>.Success(result);
    }

    private static string MakeKey(string? name, DateTime? dob)
        => $"{name?.Trim().ToLowerInvariant()}|{dob:yyyy-MM-dd}";

    private static VTOS.Domain.Enums.Gender ParseGender(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return VTOS.Domain.Enums.Gender.Other;
        return raw.Trim().ToLowerInvariant() switch
        {
            "nam" or "male" or "m"          => VTOS.Domain.Enums.Gender.Male,
            "nữ" or "nu" or "female" or "f" => VTOS.Domain.Enums.Gender.Female,
            _                               => VTOS.Domain.Enums.Gender.Other
        };
    }
}
