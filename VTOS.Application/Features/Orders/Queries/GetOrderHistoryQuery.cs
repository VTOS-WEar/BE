using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Queries;

public record GetOrderHistoryQuery(
    Guid ParentId,
    int Page,
    int PageSize,
    OrderStatus? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    string? SortBy,
    string? SortOrder,
    string? Search
);

public interface IGetOrderHistoryQueryHandler
{
    Task<Result<OrderHistoryResponse>> HandleAsync(GetOrderHistoryQuery query, CancellationToken cancellationToken = default);
}
