using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-43: Import student data from a CSV or XLSX file.
/// The caller (Controller) is responsible for parsing the file into rows.
/// Each row is string[] with 5 or 7 elements:
/// [Name, DOB, Class, Gender, Phone, HomeroomTeacherName?, HomeroomTeacherEmail?]
/// </summary>
public class ImportStudentDataCommand
{
    public Guid UserId { get; }

    /// <summary>
    /// Pre-parsed data rows (header already excluded).
    /// Each row: [StudentName, DOB (dd/MM/yyyy), Class, Gender, ParentPhone, TeacherName?, TeacherEmail?]
    /// </summary>
    public IReadOnlyList<string[]> Rows { get; }

    /// <summary>Original file name uploaded by the user.</summary>
    public string FileName { get; }

    public ImportStudentDataCommand(Guid userId, IReadOnlyList<string[]> rows, string fileName = "")
    {
        UserId = userId;
        Rows = rows;
        FileName = fileName;
    }
}

public interface IImportStudentDataCommandHandler
{
    Task<Result<ImportStudentResultDto>> HandleAsync(ImportStudentDataCommand command, CancellationToken ct = default);
}
