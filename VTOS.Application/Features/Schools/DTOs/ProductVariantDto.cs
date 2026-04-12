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
    public List<VariantMeasurementDto> Measurements { get; set; } = new();
}

public class VariantMeasurementDto
{
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Unit { get; set; } = "cm";
    public decimal? MinCm { get; set; }
    public decimal? MaxCm { get; set; }
}

public class VariantMeasurementInputDto
{
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Unit { get; set; } = "cm";
    public decimal? MinCm { get; set; }
    public decimal? MaxCm { get; set; }
}

/// <summary>
/// Request body for creating a new product variant.
/// </summary>
public class CreateVariantRequest
{
    public string Size { get; set; } = string.Empty;
    public string? ColorVariant { get; set; }
    public string? MaterialType { get; set; }
    public string? SKUCode { get; set; }
    public List<VariantMeasurementInputDto> Measurements { get; set; } = new();
}

/// <summary>
/// Request body for updating a product variant.
/// </summary>
public class UpdateVariantRequest
{
    public string? Size { get; set; }
    public string? ColorVariant { get; set; }
    public string? MaterialType { get; set; }
    public string? SKUCode { get; set; }
    public List<VariantMeasurementInputDto>? Measurements { get; set; }
}
