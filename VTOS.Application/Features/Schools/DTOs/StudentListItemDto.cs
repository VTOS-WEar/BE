namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// DTO for a single student row in the student list.
/// Combines data from ChildProfile + StudentDataImport.
/// </summary>
public class StudentListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public bool HasMeasurements { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhone { get; set; }
    public bool IsParentLinked { get; set; }
}

/// <summary>
/// Response for paginated student list.
/// </summary>
public class StudentListResponse
{
    public IReadOnlyList<StudentListItemDto> Items { get; set; } = Array.Empty<StudentListItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
