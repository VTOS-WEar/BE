using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// Full student detail DTO (for Create/Update response and GetById).
/// </summary>
public class StudentDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public Guid? ClassGroupId { get; set; }
    public string? ClassName { get; set; }
    public string? AcademicYear { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int HeightCm { get; set; }
    public float WeightKg { get; set; }
    public bool HasMeasurements { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhone { get; set; }
    public bool IsParentLinked { get; set; }
}

/// <summary>
/// Request body for creating a student.
/// </summary>
public class CreateStudentRequest
{
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Guid? ClassGroupId { get; set; }
    public string? Grade { get; set; }
    public string? Gender { get; set; }
    public string? ParentPhone { get; set; }
}

/// <summary>
/// Request body for updating a student.
/// </summary>
public class CreateOrUpdateStudentRequest : CreateStudentRequest
{
    public int? HeightCm { get; set; }
    public float? WeightKg { get; set; }
}
