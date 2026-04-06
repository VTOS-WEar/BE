namespace VTOS.Application.Features.Users.DTOs;
public record GetProfileResponse(
    Guid Id,
    string Email,
    string FullName,
    string Phone,
    DateTime DOB,
    string Gender,
    string Role,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime LastLogin,
    string? Avatar
);