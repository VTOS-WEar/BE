namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// DTO representing a product variant (size/price combination) for an outfit.
/// </summary>
public class ProductVariantDto
{
    public Guid ProductVariantId { get; set; }
    public Guid OutfitId { get; set; }
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ColorVariant { get; set; }
    public string? MaterialType { get; set; }
    public string? SKUCode { get; set; }
    public string? VariantImageURL { get; set; }
}

/// <summary>
/// Request body for creating a new product variant.
/// </summary>
public class CreateVariantRequest
{
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ColorVariant { get; set; }
    public string? MaterialType { get; set; }
    public string? SKUCode { get; set; }
}

/// <summary>
/// Request body for updating a product variant.
/// </summary>
public class UpdateVariantRequest
{
    public string? Size { get; set; }
    public decimal? Price { get; set; }
    public int? StockQuantity { get; set; }
    public string? ColorVariant { get; set; }
    public string? MaterialType { get; set; }
    public string? SKUCode { get; set; }
}
