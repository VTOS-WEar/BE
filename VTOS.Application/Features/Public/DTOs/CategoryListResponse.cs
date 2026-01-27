namespace VTOS.Application.Features.Public.DTOs;

public record CategoryListResponse(
    IEnumerable<CategoryDto> Items
);
