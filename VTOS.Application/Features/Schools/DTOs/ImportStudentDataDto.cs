namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// UC-43: Result of a student data import operation.
/// </summary>
public class ImportStudentResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = new();
}

public class ImportErrorDto
{
    public int RowNumber { get; set; }
    public string? StudentName { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
