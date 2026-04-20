namespace VTOS.Application.Features.Schools.DTOs;

public class ClassTeacherDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ClassStudentItemDto
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

public class ClassGroupSummaryDto
{
    public Guid Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string? HomeroomTeacherName { get; set; }
    public string? HomeroomTeacherEmail { get; set; }
    public int StudentCount { get; set; }
    public int MeasurementReadyCount { get; set; }
    public int ParentLinkedCount { get; set; }
}

public class GradeClassGroupDto
{
    public string Grade { get; set; } = string.Empty;
    public int ClassCount { get; set; }
    public int StudentCount { get; set; }
    public IReadOnlyList<ClassGroupSummaryDto> Classes { get; set; } = Array.Empty<ClassGroupSummaryDto>();
}

public class SchoolClassesOverviewDto
{
    public string AcademicYear { get; set; } = string.Empty;
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int UnassignedStudentCount { get; set; }
    public IReadOnlyList<GradeClassGroupDto> Grades { get; set; } = Array.Empty<GradeClassGroupDto>();
}

public class TeacherClassesOverviewDto
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public IReadOnlyList<ClassGroupSummaryDto> Classes { get; set; } = Array.Empty<ClassGroupSummaryDto>();
}

public class ClassGroupDetailDto
{
    public Guid Id { get; set; }
    public Guid SchoolID { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public ClassTeacherDto? HomeroomTeacher { get; set; }
    public int StudentCount { get; set; }
    public int MeasurementReadyCount { get; set; }
    public int ParentLinkedCount { get; set; }
    public IReadOnlyList<ClassStudentItemDto> Students { get; set; } = Array.Empty<ClassStudentItemDto>();
}
