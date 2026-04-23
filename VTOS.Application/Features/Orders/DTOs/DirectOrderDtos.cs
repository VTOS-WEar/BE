using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.DTOs;

public class DirectOrderItemRequest
{
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public bool IsCustomOrder { get; set; }
    public string? CustomMeasurements { get; set; }
}

public class CreateDirectOrderRequest
{
    public Guid ChildProfileId { get; set; }
    public Guid SemesterPublicationId { get; set; }
    public Guid ProviderId { get; set; }
    public List<DirectOrderItemRequest> Items { get; set; } = new();
    public string ShippingAddress { get; set; } = string.Empty;
    public string? DeliveryMethod { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
}

public class CreateDirectOrderResponse
{
    public Guid OrderId { get; set; }
    public Guid PaymentTransactionId { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentLink { get; set; } = string.Empty;
    public int OrderCode { get; set; }
}

public class MyDirectOrdersResponse
{
    public List<MyDirectOrderListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class MyDirectOrderListItemDto
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public string OrderStatusName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string PricingMode { get; set; } = string.Empty;
    public string? FirstItemImageUrl { get; set; }
    public string? PaymentStatusName { get; set; }
    public string? TrackingCode { get; set; }
}

public class MyDirectOrderDetailDto
{
    public Guid OrderId { get; set; }
    public Guid ChildProfileId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public Guid SemesterPublicationId { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string PricingMode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? DeliveryMethod { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? TrackingCode { get; set; }
    public string? ShippingCompany { get; set; }
    public string? PaymentStatusName { get; set; }
    public bool CanRateProvider { get; set; }
    public ExistingProviderRatingDto? ExistingProviderRating { get; set; }
    public List<MyDirectOrderDetailItemDto> Items { get; set; } = new();
}

public class ExistingProviderRatingDto
{
    public Guid ProviderRatingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MyDirectOrderDetailItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid ProductVariantId { get; set; }
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SubmitProviderRatingRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class SubmitProviderRatingResponse
{
    public Guid ProviderRatingId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProviderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
