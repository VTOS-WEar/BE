namespace VTOS.Application.Features.Admin.DTOs;

public record UserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? LastLogin,
    string? Phone,
    string? SchoolName,
    int ChildrenCount
);