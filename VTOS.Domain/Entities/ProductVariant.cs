using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a product variant (size, color, material combination).
/// Maps to the ProductVariant table in the database.
/// </summary>
public class ProductVariant : BaseEntity
{
    public Guid OutfitID { get; set; }
    public string Size { get; set; } = string.Empty;
    public string? ColorVariant { get; set; }
    public string? MaterialType { get; set; }
    public int StockQuantity { get; set; }
    public decimal Price { get; set; }
    public string? SKUCode { get; set; }
    public string? VariantImageURL { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Outfit Outfit { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
