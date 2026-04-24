namespace VTOS.Application.Features.Users.DTOs;

/// <summary>
/// DTO for a child profile returned to a parent.
/// </summary>
public record ChildProfileDto(
    Guid ChildId,
    string FullName,
    int Age,
    string Grade,
    Guid? ClassGroupId,
    string? ClassName,
    string? AcademicYear,
    string Gender,
    string? AvatarUrl,
    ChildSchoolDto School,
    int HeightCm,
    float WeightKg
);

/// <summary>
/// School info embedded in child profile DTO.
/// </summary>
public record ChildSchoolDto(
    Guid SchoolId,
    string SchoolName,
    string? LogoURL
);
