namespace VTOS.Application.Features.Admin.DTOs;

public record CategoryDto(
    Guid Id,
    string CategoryName,
    DateTime CreatedAt
);
