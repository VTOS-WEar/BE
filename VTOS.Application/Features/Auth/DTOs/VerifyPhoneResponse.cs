namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Response DTO for phone verification.
/// Contains list of children matched from StudentDataImport.
/// </summary>
public record VerifyPhoneResponse(
    string Phone,
    int MatchedCount,
    List<ChildDto> Children,
    string Message
);

/// <summary>
/// Child information DTO.
/// </summary>
public record ChildDto(
    Guid ChildId,
    string FullName,
    int Age,
    string Grade,
    string Gender,
    SchoolDto School
);

/// <summary>
/// School information DTO.
/// </summary>
public record SchoolDto(
    Guid SchoolId,
    string SchoolName,
    string? LogoURL
);
