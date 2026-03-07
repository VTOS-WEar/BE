using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.DTOs;

public class OrderStatusResponse
{
    public Guid OrderId { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public string OrderStatusName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? DeliveryMethod { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? PaymentStatusName { get; set; }
    public List<OrderItemDetail> Items { get; set; } = new();
}

public class OrderItemDetail
{
    public Guid ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKUCode { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
