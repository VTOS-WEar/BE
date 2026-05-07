namespace VTOS.Application.Features.Admin.DTOs;

public record UserListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedAt,
    string? SchoolName = null,
    string? ProviderName = null
);
