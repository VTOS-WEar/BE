namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// Response DTO for school profile (UC-42).
/// </summary>
public class SchoolProfileDto
{
    public Guid Id { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string? LogoURL { get; set; }
    public string? ContactInfo { get; set; }
    public Guid? CatalogID { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for updating school profile (UC-42).
/// </summary>
public record UpdateSchoolProfileRequest(
    string? SchoolName,
    string? LogoURL,
    string? ContactInfo
);
