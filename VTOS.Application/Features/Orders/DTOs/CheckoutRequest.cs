namespace VTOS.Application.Features.Orders.DTOs;

/// <summary>
/// Represents a checkout item with product variant and quantity
/// </summary>
public class CheckoutItemRequest
{
    /// <summary>
    /// Product variant ID to checkout
    /// </summary>
    public Guid ProductVariantId { get; set; }

    /// <summary>
    /// Quantity to purchase
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Flag for custom order
    /// </summary>
    public bool IsCustomOrder { get; set; } = false;

    /// <summary>
    /// Custom measurements if IsCustomOrder is true (JSON format)
    /// </summary>
    public string? CustomMeasurements { get; set; } = string.Empty;
}

/// <summary>
/// Represents a checkout request when user clicks checkout
/// </summary>
public class CheckoutRequest
{
    /// <summary>
    /// Child profile ID for the order
    /// </summary>
    public Guid ChildProfileId { get; set; }

    /// <summary>
    /// List of items to checkout
    /// </summary>
    public List<CheckoutItemRequest> Items { get; set; } = new();

    /// <summary>
    /// Shipping address
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Delivery method (e.g., "Standard", "Express")
    /// </summary>
    public string? DeliveryMethod { get; set; }

    /// <summary>
    /// Optional campaign ID for promotional orders
    /// </summary>
    public Guid? CampaignId { get; set; }
}
