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
/// Request body for creating/updating a student.
/// </summary>
public class CreateOrUpdateStudentRequest
{
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Grade { get; set; }
    public string? Gender { get; set; }  // "Nam"/"Nữ"/"Male"/"Female"
    public string? ParentPhone { get; set; }
    public int? HeightCm { get; set; }
    public float? WeightKg { get; set; }
}
