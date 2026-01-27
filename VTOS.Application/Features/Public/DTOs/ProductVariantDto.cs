namespace VTOS.Application.Features.Public.DTOs;

public record ProductVariantDto(
    Guid ProductVariantId,
    string Size,
    string? ColorVariant,
    string? MaterialType,
    int StockQuantity,
    decimal Price,
    string? SkuCode,
    string? VariantImageURL
);
