using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;

namespace VTOS.Application.Features.Orders.Commands;

/// <summary>
/// Command for user checkout that creates Order and PaymentTransaction
/// </summary>
public record CheckoutCommand(
    Guid ParentId,

    Guid ChildProfileId,

   List<CheckoutItemRequest> Items,

    string ShippingAddress,

    string? DeliveryMethod,

      Guid? CampaignId
);

/// <summary>
/// Handler interface for CheckoutCommand
/// </summary>
public interface ICheckoutCommandHandler
{
    Task<Result<CheckoutResponse>> HandleAsync(CheckoutCommand command, CancellationToken cancellationToken = default);
}
