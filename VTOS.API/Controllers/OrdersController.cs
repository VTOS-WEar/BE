using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.Commands;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Application.Features.Orders.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Parent")]
public class OrdersController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICheckoutCommandHandler _checkoutCommandHandler;
    private readonly ICancelOrderCommandHandler _cancelOrderCommandHandler;
    private readonly IGetOrderStatusQueryHandler _getOrderStatusQueryHandler;
    private readonly IGetOrderHistoryQueryHandler _getOrderHistoryQueryHandler;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        ICurrentUserService currentUserService,
        ICheckoutCommandHandler checkoutCommandHandler,
        ICancelOrderCommandHandler cancelOrderCommandHandler,
        IGetOrderStatusQueryHandler getOrderStatusQueryHandler,
        IGetOrderHistoryQueryHandler getOrderHistoryQueryHandler,
        ILogger<OrdersController> logger)
    {
        _currentUserService = currentUserService;
        _checkoutCommandHandler = checkoutCommandHandler;
        _cancelOrderCommandHandler = cancelOrderCommandHandler;
        _getOrderStatusQueryHandler = getOrderStatusQueryHandler;
        _getOrderHistoryQueryHandler = getOrderHistoryQueryHandler;
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

    /// <summary>
    /// Cancel an order and its payment transaction
    /// </summary>
    /// <remarks>
    /// This endpoint handles the cancel order flow:
    /// 1. Validates the order belongs to the current parent
    /// 2. Checks the order is cancellable (Pending or Paid)
    /// 3. If Pending: cancels the PayOS payment link
    /// 4. If Paid: creates a refund request
    /// 5. Updates Order and PaymentTransaction status to Cancelled
    /// </remarks>
    /// <param name="orderId">The ID of the order to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    [HttpPut("{orderId}/cancel")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelOrder(
        [FromRoute] Guid orderId,
        [FromBody] CancelOrderRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Cancel order initiated: OrderId={OrderId}", orderId);

            var command = new CancelOrderCommand(_currentUserService.UserId, orderId, request?.Reason);
            var result = await _cancelOrderCommandHandler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Cancel order failed: {Error}", result.Error);
                return result.ErrorCode == "ORDER_NOT_FOUND"
                    ? NotFound(result)
                    : BadRequest(result);
            }

            _logger.LogInformation("Order cancelled successfully: OrderId={OrderId}", orderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during order cancellation");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Result.Failure(
                    "An unexpected error occurred during order cancellation",
                    "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// Get order status and details
    /// </summary>
    /// <param name="orderId">The ID of the order to track</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order status with item details and payment status</returns>
    [HttpGet("{orderId}/status")]
    [ProducesResponseType(typeof(Result<OrderStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderStatus(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetOrderStatusQuery(_currentUserService.UserId, orderId);
            var result = await _getOrderStatusQueryHandler.HandleAsync(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ErrorCode == "ORDER_NOT_FOUND"
                    ? NotFound(result)
                    : BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting order status");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Result<OrderStatusResponse>.Failure(
                    "An unexpected error occurred",
                    "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// View order history with filtering, pagination, and sorting
    /// </summary>
    /// <remarks>
    /// Query parameters:
    /// - page (default: 1)
    /// - pageSize (default: 10, max: 50)
    /// - status: filter by OrderStatus enum (1=Pending, 2=Paid, 3=Confirmed, ...)
    /// - fromDate / toDate: filter by order date range
    /// - sortBy: "orderDate" (default), "totalAmount", "status"
    /// - sortOrder: "asc" or "desc" (default: "desc")
    /// - search: search by shipping address
    /// </remarks>
    /// <returns>Paginated list of orders</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(Result<OrderHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderHistory(
        [FromQuery] OrderHistoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetOrderHistoryQuery(
                _currentUserService.UserId,
                request.Page,
                request.PageSize,
                request.Status,
                request.FromDate,
                request.ToDate,
                request.SortBy,
                request.SortOrder,
                request.Search
            );

            var result = await _getOrderHistoryQueryHandler.HandleAsync(query, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting order history");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Result<OrderHistoryResponse>.Failure(
                    "An unexpected error occurred",
                    "INTERNAL_SERVER_ERROR"));
        }
    }
}