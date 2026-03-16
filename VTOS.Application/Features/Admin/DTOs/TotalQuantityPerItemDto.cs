namespace VTOS.Application.Features.Admin.DTOs;

public record TotalQuantityPerItemDto(
    List<ItemQuantitySoldDto> Items
);

public record ItemQuantitySoldDto(
    Guid OutfitId,
    string UniformName,
    string Size,
    int QuantitySold,
    Guid SchoolId,
    string SchoolName
);
