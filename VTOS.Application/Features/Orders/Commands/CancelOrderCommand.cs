using VTOS.Application.Common;

namespace VTOS.Application.Features.Orders.Commands;

/// <summary>
/// Command to cancel an order, update statuses, and cancel PayOS payment link
/// </summary>
public record CancelOrderCommand(
    Guid ParentId,
    Guid OrderId
);

/// <summary>
/// Handler interface for CancelOrderCommand
/// </summary>
public interface ICancelOrderCommandHandler
{
    Task<Result> HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default);
}
