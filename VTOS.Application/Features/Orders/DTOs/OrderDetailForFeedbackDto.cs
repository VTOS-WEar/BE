namespace VTOS.Application.Features.Orders.DTOs;

public class OrderDetailForFeedbackDto
{
    public Guid OrderId { get; set; }
    public Guid ChildProfileId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string? ChildAvatar { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? CampaignName { get; set; }
    public Guid? CampaignId { get; set; }
    public string? ProviderName { get; set; }
    public Guid? ProviderId { get; set; }
    public List<OrderItemForFeedbackDto> Items { get; set; } = new();
}

public class OrderItemForFeedbackDto
{
    public Guid OrderItemId { get; set; }
    public Guid? CampaignOutfitId { get; set; }
    public Guid ProductVariantId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string? OutfitImage { get; set; }
    public int Quantity { get; set; }
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
