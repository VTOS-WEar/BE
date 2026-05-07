using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.DTOs;

public class OrderHistoryResponse
{
    public List<OrderHistoryItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class OrderHistoryItem
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public string OrderStatusName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? DeliveryMethod { get; set; }
    public int ItemCount { get; set; }
    public string? PaymentStatusName { get; set; }
    public string? ChildName { get; set; }
    public string? FirstItemImageUrl { get; set; }
    public string? SchoolName { get; set; }
}
