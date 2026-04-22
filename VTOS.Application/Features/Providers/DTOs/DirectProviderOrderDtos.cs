namespace VTOS.Application.Features.Providers.DTOs;

public class ProviderIncomingOrdersResponse
{
    public List<ProviderIncomingOrderItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ProviderIncomingOrderItemDto
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ChildName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string? TrackingCode { get; set; }
}

public class ProviderDirectOrderDetailDto
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string? ParentPhone { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? DeliveryMethod { get; set; }
    public string? TrackingCode { get; set; }
    public string? ShippingCompany { get; set; }
    public List<ProviderDirectOrderDetailItemDto> Items { get; set; } = new();
}

public class ProviderDirectOrderDetailItemDto
{
    public Guid OrderItemId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ProviderOrderStatsDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int PaidOrders { get; set; }
    public int InProgressOrders { get; set; }
    public int CompletedShipmentOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}
