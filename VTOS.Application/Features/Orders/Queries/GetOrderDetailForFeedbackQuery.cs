using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;

namespace VTOS.Application.Features.Orders.Queries;

public record GetOrderDetailForFeedbackQuery(
    Guid ParentId,
    Guid OrderId
);

public interface IGetOrderDetailForFeedbackQueryHandler
{
    Task<Result<OrderDetailForFeedbackDto>> HandleAsync(GetOrderDetailForFeedbackQuery query, CancellationToken cancellationToken = default);
}
