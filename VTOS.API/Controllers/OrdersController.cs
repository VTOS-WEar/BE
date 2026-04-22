using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.Commands;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Application.Features.Orders.Queries;
using VTOS.Application.Features.Providers.Commands;
using VTOS.Application.Features.Providers.DTOs;

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
    private readonly IGetOrderDetailForFeedbackQueryHandler _getOrderDetailForFeedbackQueryHandler;
    private readonly IRetryPaymentCommandHandler _retryPaymentCommandHandler;
    private readonly ICancelPaymentTransactionCommandHandler _cancelPaymentTransactionCommandHandler;
    private readonly ICreateDirectOrderCommandHandler _createDirectOrderCommandHandler;
    private readonly IGetMyDirectOrdersQueryHandler _getMyDirectOrdersQueryHandler;
    private readonly IGetMyDirectOrderDetailQueryHandler _getMyDirectOrderDetailQueryHandler;
    private readonly ICancelDirectOrderCommandHandler _cancelDirectOrderCommandHandler;
    private readonly IConfirmDirectOrderDeliveryCommandHandler _confirmDirectOrderDeliveryCommandHandler;
    private readonly ISubmitProviderRatingCommandHandler _submitProviderRatingCommandHandler;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        ICurrentUserService currentUserService,
        ICheckoutCommandHandler checkoutCommandHandler,
        ICancelOrderCommandHandler cancelOrderCommandHandler,
        IGetOrderStatusQueryHandler getOrderStatusQueryHandler,
        IGetOrderHistoryQueryHandler getOrderHistoryQueryHandler,
        IGetOrderDetailForFeedbackQueryHandler getOrderDetailForFeedbackQueryHandler,
        IRetryPaymentCommandHandler retryPaymentCommandHandler,
        ICancelPaymentTransactionCommandHandler cancelPaymentTransactionCommandHandler,
        ICreateDirectOrderCommandHandler createDirectOrderCommandHandler,
        IGetMyDirectOrdersQueryHandler getMyDirectOrdersQueryHandler,
        IGetMyDirectOrderDetailQueryHandler getMyDirectOrderDetailQueryHandler,
        ICancelDirectOrderCommandHandler cancelDirectOrderCommandHandler,
        IConfirmDirectOrderDeliveryCommandHandler confirmDirectOrderDeliveryCommandHandler,
        ISubmitProviderRatingCommandHandler submitProviderRatingCommandHandler,
        ILogger<OrdersController> logger)
    {
        _currentUserService = currentUserService;
        _checkoutCommandHandler = checkoutCommandHandler;
        _cancelOrderCommandHandler = cancelOrderCommandHandler;
        _getOrderStatusQueryHandler = getOrderStatusQueryHandler;
        _getOrderHistoryQueryHandler = getOrderHistoryQueryHandler;
        _getOrderDetailForFeedbackQueryHandler = getOrderDetailForFeedbackQueryHandler;
        _retryPaymentCommandHandler = retryPaymentCommandHandler;
        _cancelPaymentTransactionCommandHandler = cancelPaymentTransactionCommandHandler;
        _createDirectOrderCommandHandler = createDirectOrderCommandHandler;
        _getMyDirectOrdersQueryHandler = getMyDirectOrdersQueryHandler;
        _getMyDirectOrderDetailQueryHandler = getMyDirectOrderDetailQueryHandler;
        _cancelDirectOrderCommandHandler = cancelDirectOrderCommandHandler;
        _confirmDirectOrderDeliveryCommandHandler = confirmDirectOrderDeliveryCommandHandler;
        _submitProviderRatingCommandHandler = submitProviderRatingCommandHandler;
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
            return Ok(new { message = "Order cancelled successfully.", refunds = result.Value });
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

    /// <summary>
    /// Get order details specifically for feedback submission
    /// </summary>
    /// <remarks>
    /// This endpoint returns order details with campaign outfit information for feedback collection.
    /// Includes child information, order items with campaign outfit IDs for targeting feedback submission.
    /// </remarks>
    /// <param name="orderId">The ID of the order to retrieve details for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order details with items and campaign outfit information</returns>
    [HttpGet("{orderId}/detail")]
    [ProducesResponseType(typeof(Result<OrderDetailForFeedbackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderDetailForFeedback(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetOrderDetailForFeedbackQuery(_currentUserService.UserId, orderId);
            var result = await _getOrderDetailForFeedbackQueryHandler.HandleAsync(query, cancellationToken);

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
            _logger.LogError(ex, "Unexpected error getting order details for feedback");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Result<OrderDetailForFeedbackDto>.Failure(
                    "An unexpected error occurred",
                    "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// Retry payment for a cancelled or pending order.
    /// Creates a new PayOS payment link and resets the order to Pending.
    /// </summary>
    /// <param name="orderId">The ID of the order to retry payment for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New payment link and transaction details</returns>
    [HttpPost("{orderId}/retry-payment")]
    [ProducesResponseType(typeof(Result<RetryPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryPayment(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retry payment initiated: OrderId={OrderId}", orderId);

            var command = new RetryPaymentCommand(_currentUserService.UserId, orderId);
            var result = await _retryPaymentCommandHandler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Retry payment failed: {Error}", result.Error);
                return result.ErrorCode == "ORDER_NOT_FOUND"
                    ? NotFound(result)
                    : BadRequest(result);
            }

            _logger.LogInformation("Retry payment created: OrderId={OrderId}", orderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during retry payment");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Result<RetryPaymentResponse>.Failure(
                    "An unexpected error occurred during retry payment",
                    "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// Cancel only the payment transaction, keeping the order in Pending state.
    /// Useful when the user returns from PayOS via cancel flow.
    /// </summary>
    [HttpPut("{orderId}/cancel-transaction")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPaymentTransaction(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CancelPaymentTransactionCommand(_currentUserService.UserId, orderId);
            var result = await _cancelPaymentTransactionCommandHandler.HandleAsync(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.ErrorCode == "ORDER_NOT_FOUND" ? NotFound(result) : BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during cancel-transaction");
            return StatusCode(StatusCodes.Status500InternalServerError, Result.Failure("Internal server error"));
        }
    }

    [HttpPost("direct")]
    [ProducesResponseType(typeof(Result<CreateDirectOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDirectOrder([FromBody] CreateDirectOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _createDirectOrderCommandHandler.HandleAsync(
            new CreateDirectOrderCommand(
                _currentUserService.UserId,
                request.ChildProfileId,
                request.SemesterPublicationId,
                request.ProviderId,
                request.Items,
                request.ShippingAddress,
                request.DeliveryMethod,
                request.RecipientName,
                request.RecipientPhone),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(Result<MyDirectOrdersResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDirectOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _getMyDirectOrdersQueryHandler.HandleAsync(
            new GetMyDirectOrdersQuery(_currentUserService.UserId, page, pageSize, status),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("my-orders/{id:guid}")]
    [ProducesResponseType(typeof(Result<MyDirectOrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyDirectOrderDetail(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _getMyDirectOrderDetailQueryHandler.HandleAsync(
            new GetMyDirectOrderDetailQuery(_currentUserService.UserId, id),
            cancellationToken);

        if (!result.IsSuccess)
            return result.ErrorCode == "ORDER_NOT_FOUND" ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{orderId:guid}/cancel-direct")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelDirectOrder(Guid orderId, [FromBody] CancelOrderRequest? request, CancellationToken cancellationToken = default)
    {
        var result = await _cancelDirectOrderCommandHandler.HandleAsync(
            new CancelDirectOrderCommand(_currentUserService.UserId, orderId, request?.Reason),
            cancellationToken);

        if (!result.IsSuccess)
            return result.ErrorCode == "ORDER_NOT_FOUND" ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{orderId:guid}/confirm-delivery")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmDirectOrderDelivery(Guid orderId, CancellationToken cancellationToken = default)
    {
        var result = await _confirmDirectOrderDeliveryCommandHandler.HandleAsync(
            new ConfirmDirectOrderDeliveryCommand(_currentUserService.UserId, orderId),
            cancellationToken);

        if (!result.IsSuccess)
            return result.ErrorCode == "ORDER_NOT_FOUND" ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{orderId:guid}/rate-provider")]
    [ProducesResponseType(typeof(Result<SubmitProviderRatingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RateProvider(Guid orderId, [FromBody] SubmitProviderRatingRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _submitProviderRatingCommandHandler.HandleAsync(
            new SubmitProviderRatingCommand(_currentUserService.UserId, orderId, request.Rating, request.Comment),
            cancellationToken);

        if (!result.IsSuccess)
            return result.ErrorCode == "ORDER_NOT_FOUND" ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }
}

public class SubmitProviderRatingRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
