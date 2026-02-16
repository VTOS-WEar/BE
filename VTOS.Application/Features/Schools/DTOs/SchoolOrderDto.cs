namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// DTO for a single order in the school's order list (UC-45).
/// </summary>
public class SchoolOrderDto
{
    public Guid OrderId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ChildName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string? CampaignName { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Paginated response for school orders (UC-45).
/// </summary>
public class SchoolOrderListResponse
{
    public List<SchoolOrderDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
