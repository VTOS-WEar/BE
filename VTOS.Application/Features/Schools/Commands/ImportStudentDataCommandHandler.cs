using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-43: Import student data from CSV.
/// - UTF-8 with BOM detection for Vietnamese
/// - Row 1 = header (skipped), Row 2+ = data
/// - Columns: Student Name, DOB, Grade, Gender, Parent Phone Number
/// - Duplicate detection by FullName + DOB within same school
/// - Row-level error reporting (doesn't fail entire import)
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
        // 1. Resolve school from current user
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null || user.SchoolID == null)
            return Result<ImportStudentResultDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolId = user.SchoolID.Value;

        // 2. Load existing imports for duplicate detection
        var existingImports = await _db.StudentDataImports
            .AsNoTracking()
            .Where(s => s.SchoolID == schoolId)
            .Select(s => new { s.FullName, s.DateOfBirth })
            .ToListAsync(ct);

        var existingSet = existingImports
            .Select(e => $"{e.FullName?.Trim().ToLowerInvariant()}|{e.DateOfBirth:yyyy-MM-dd}")
            .ToHashSet();

        // 3. Parse CSV
        var result = new ImportStudentResultDto();
        var newRecords = new List<StudentDataImport>();
        var errors = new List<ImportErrorDto>();

        using var reader = new StreamReader(command.CsvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        // Skip header row
        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine == null)
            return Result<ImportStudentResultDto>.Failure("CSV file is empty.", "CSV_EMPTY");

        int rowNumber = 1; // header is row 1

        while (!reader.EndOfStream)
        {
            rowNumber++;
            var line = await reader.ReadLineAsync(ct);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            result.TotalRows++;

            try
            {
                var columns = ParseCsvLine(line);

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

                var fullName = columns[0].Trim();
                var dobStr = columns[1].Trim();
                var grade = columns[2].Trim();
                var gender = columns[3].Trim();
                var parentPhone = columns[4].Trim();

                // Validate: FullName is required
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        StudentName = null,
                        ErrorMessage = "Student Name is required."
                    });
                    result.ErrorCount++;
                    continue;
                }

                // Validate: FullName max length
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

                // Parse DOB (dd/MM/yyyy)
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

                // Normalize phone number
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

                // Validate gender
                if (!string.IsNullOrWhiteSpace(gender) && gender.Length > 10)
                {
                    gender = gender[..10];
                }

                // Duplicate detection
                var key = $"{fullName.Trim().ToLowerInvariant()}|{dob:yyyy-MM-dd}";
                if (existingSet.Contains(key))
                {
                    result.SkippedCount++;
                    continue;
                }

                // Also check within current batch
                if (newRecords.Any(r =>
                    r.FullName.Trim().Equals(fullName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    r.DateOfBirth == dob))
                {
                    result.SkippedCount++;
                    continue;
                }

                // Create record
                var record = new StudentDataImport
                {
                    Id = Guid.NewGuid(),
                    SchoolID = schoolId,
                    FullName = fullName,
                    DateOfBirth = dob,
                    Class = string.IsNullOrWhiteSpace(grade) ? null : grade,
                    Gender = string.IsNullOrWhiteSpace(gender) ? null : gender,
                    ParentPhone = string.IsNullOrWhiteSpace(parentPhone) ? null : parentPhone,
                    IsRegistered = false,
                    CreatedAt = DateTime.UtcNow
                };

                newRecords.Add(record);
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

        // 4. Batch insert
        if (newRecords.Count > 0)
        {
            _db.StudentDataImports.AddRange(newRecords);
            await _db.SaveChangesAsync(ct);
        }

        result.Errors = errors;
        return Result<ImportStudentResultDto>.Success(result);
    }

    /// <summary>
    /// Simple CSV line parser that handles quoted fields (for commas inside values).
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
