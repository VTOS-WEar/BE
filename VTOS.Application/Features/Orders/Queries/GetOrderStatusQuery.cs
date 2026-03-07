using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;

namespace VTOS.Application.Features.Orders.Queries;

public record GetOrderStatusQuery(
    Guid ParentId,
    Guid OrderId
);

public interface IGetOrderStatusQueryHandler
{
    Task<Result<OrderStatusResponse>> HandleAsync(GetOrderStatusQuery query, CancellationToken cancellationToken = default);
}
