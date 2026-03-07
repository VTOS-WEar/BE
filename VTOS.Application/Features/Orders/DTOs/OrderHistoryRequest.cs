using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.DTOs;

public class OrderHistoryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public OrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; }        // "orderDate", "totalAmount", "status"
    public string? SortOrder { get; set; }      // "asc" or "desc" (default: "desc")
    public string? Search { get; set; }         // Search by shipping address
}