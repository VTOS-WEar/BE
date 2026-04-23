using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

public record GetSchoolOrdersQuery(
    Guid SchoolId,
    int Page = 1,
    int PageSize = 10,
    string? Status = null,
    string? Search = null);

public interface IGetSchoolOrdersQueryHandler
{
    Task<Result<SchoolOrderListResponse>> HandleAsync(GetSchoolOrdersQuery query, CancellationToken ct = default);
}

public class SchoolOrderListResponse
{
    public List<SchoolOrderDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SchoolOrderDto
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? TrackingCode { get; set; }
    public string? ShippingCompany { get; set; }
    public string? DeliveryMethod { get; set; }
    public string? LatestPaymentStatus { get; set; }
    public int TotalQuantity { get; set; }
    public List<SchoolOrderItemDto> Items { get; set; } = new();
}

public class SchoolOrderItemDto
{
    public Guid ProductVariantId { get; set; }
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
