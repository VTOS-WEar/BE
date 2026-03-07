using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.Commands;
using VTOS.Application.Features.Orders.DTOs;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Parent")]
public class OrdersController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICheckoutCommandHandler _checkoutCommandHandler;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        ICurrentUserService currentUserService,
        ICheckoutCommandHandler checkoutCommandHandler,
        ILogger<OrdersController> logger)
    {
        _currentUserService = currentUserService;
        _checkoutCommandHandler = checkoutCommandHandler;
        _logger = logger;
    }

    /// <summary>
    /// Create order and payment transaction when user clicks checkout
    /// </summary>
    /// <remarks>
    /// This endpoint handles the checkout flow:
    /// 1. Validates all items in cart
    /// 2. Calculates total price
    /// 3. Creates Order with status = PENDING
    /// 4. Creates PaymentTransaction with status = PENDING
    /// 5. Generates PayOS payment link
    /// 
    /// Sample request:
    /// 
    ///     POST /api/orders/checkout
    ///     {
    ///         "childProfileId": "550e8400-e29b-41d4-a716-446655440000",
    ///         "items": [
    ///             {
    ///                 "productVariantId": "550e8400-e29b-41d4-a716-446655440001",
    ///                 "quantity": 2,
    ///                 "sizeOrdered": "M",
    ///                 "isCustomOrder": false,
    ///                 "customMeasurements": null
    ///             }
    ///         ],
    ///         "shippingAddress": "123 Main St, City, Country",
    ///         "deliveryMethod": "Standard",
    ///         "campaignId": null
    ///     }
    /// </remarks>
    /// <param name="request">Checkout request with items and shipping details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Checkout response with order details and payment link</returns>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(Result<CheckoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (request == null)
            {
                _logger.LogWarning("Checkout: Request is null");
                return BadRequest(Result<CheckoutResponse>.Failure("Request cannot be empty", "INVALID_REQUEST"));
            }

            _logger.LogInformation("Checkout initiated for child profile: {ChildProfileId} with {ItemCount} items",
                request.ChildProfileId, request.Items?.Count ?? 0);

            // Execute checkout command
            var command = new CheckoutCommand(_currentUserService.UserId, request.ChildProfileId, request.Items, request.ShippingAddress, request.DeliveryMethod, request.CampaignId);
            var result = await _checkoutCommandHandler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Checkout failed: {Error}", result.Error);
                return BadRequest(result);
            }

            _logger.LogInformation("Checkout completed successfully: OrderId={OrderId}", result.Value?.OrderId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during checkout");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Result<CheckoutResponse>.Failure(
                    "An unexpected error occurred during checkout",
                    "INTERNAL_SERVER_ERROR"));
        }
    }
}