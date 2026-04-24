using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;

namespace VTOS.Application.Features.Schools.Services;

public class StudentCodeGenerator : IStudentCodeGenerator
{
    private readonly IApplicationDbContext _db;

    public StudentCodeGenerator(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(
        Guid schoolId,
        string className,
        IEnumerable<string>? reservedCodes = null,
        CancellationToken ct = default)
    {
        var schoolLevel = await _db.Schools
            .AsNoTracking()
            .Where(s => s.Id == schoolId)
            .Select(s => s.Level)
            .FirstOrDefaultAsync(ct);

        var schoolCode = ToCodeSegment(schoolLevel, "SCH");
        var classCode = ToCodeSegment(className, "CLASS");
        var prefix = $"{schoolCode}-{classCode}-";

        var existingCodes = await _db.StudentDataImports
            .AsNoTracking()
            .Where(s => s.SchoolID == schoolId && s.StudentCode != null && s.StudentCode.StartsWith(prefix))
            .Select(s => s.StudentCode!)
            .ToListAsync(ct);

        if (reservedCodes != null)
        {
            existingCodes.AddRange(reservedCodes.Where(code => code.StartsWith(prefix, StringComparison.Ordinal)));
        }

        var nextSequence = existingCodes
            .Select(code => code.Length > prefix.Length ? code[prefix.Length..] : string.Empty)
            .Select(suffix => int.TryParse(suffix, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{nextSequence:00}";
    }

    private static string ToCodeSegment(string? raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;

        var chars = raw
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return chars.Length == 0 ? fallback : new string(chars);
    }
}
