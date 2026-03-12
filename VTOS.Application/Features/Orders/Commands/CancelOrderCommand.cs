using VTOS.Application.Common;
using VTOS.Application.Common.Models;

namespace VTOS.Application.Features.Orders.Commands;

/// <summary>
/// Command to cancel an order, update statuses, and cancel PayOS payment link
/// </summary>
public record CancelOrderCommand(
    Guid ParentId,
    Guid OrderId,
    string? Reason = null
);

/// <summary>
/// Handler interface for CancelOrderCommand
/// </summary>
public interface ICancelOrderCommandHandler
{
    Task<Result<List<RefundResponse>>> HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default);
}
