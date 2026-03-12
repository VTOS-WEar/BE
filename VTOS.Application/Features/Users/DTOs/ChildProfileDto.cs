namespace VTOS.Application.Features.Users.DTOs;

/// <summary>
/// DTO for a child profile returned to a parent.
/// </summary>
public record ChildProfileDto(
    Guid ChildId,
    string FullName,
    int Age,
    string Grade,
    string Gender,
    ChildSchoolDto School
);

/// <summary>
/// School info embedded in child profile DTO.
/// </summary>
public record ChildSchoolDto(
    Guid SchoolId,
    string SchoolName,
    string? LogoURL
);
