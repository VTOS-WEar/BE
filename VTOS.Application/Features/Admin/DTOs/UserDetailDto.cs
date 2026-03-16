namespace VTOS.Application.Features.Admin.DTOs;

public record UserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    DateTime? DOB,
    string Gender,
    string Avatar,
    string Role,
    Guid? SchoolId,
    string? SchoolName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLogin,
    // Related info for Parent
    int ChildrenCount,
    decimal TotalSpending
);
