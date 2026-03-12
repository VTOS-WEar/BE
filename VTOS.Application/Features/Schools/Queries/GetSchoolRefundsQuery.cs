using VTOS.Application.Common;
using VTOS.Application.Common.Models;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// Query for school to get refund requests related to their school.
/// </summary>
public record GetSchoolRefundsQuery(
    Guid SchoolUserId,
    int Page = 1,
    int PageSize = 10,
    string? Status = null);

public interface IGetSchoolRefundsQueryHandler
{
    Task<Result<SchoolRefundListResponse>> HandleAsync(GetSchoolRefundsQuery query, CancellationToken ct = default);
}

public class SchoolRefundListResponse
{
    public List<SchoolRefundDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SchoolRefundDto
{
    public Guid RefundId { get; set; }
    public Guid OrderId { get; set; }
    public Guid PaymentTransactionId { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundStatus { get; set; } = string.Empty;
    public string? DisputeReason { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ChildName { get; set; } = string.Empty;
    public decimal OrderTotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
