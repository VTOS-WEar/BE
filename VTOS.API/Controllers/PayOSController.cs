using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Models;
using VTOS.Application.Features.Orders.Commands;
using VTOS.Application.Features.Orders.DTOs;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/payos")]
[Authorize(Roles = "Parent,Admin")]
public class PayOSController : ControllerBase
{
    private readonly IPayOSService _payOSService;
    private readonly IPaymentWebhookHandler _paymentWebhookHandler;
    private readonly ILogger<PayOSController> _logger;
    private const string InternalServerError = "INTERNAL_SERVER_ERROR";

    public PayOSController(
        IPayOSService payOSService,
        IPaymentWebhookHandler paymentWebhookHandler,
        ILogger<PayOSController> logger)
    {
        _payOSService = payOSService;
        _paymentWebhookHandler = paymentWebhookHandler;
        _logger = logger;
    }

    /// <summary>
    /// Create a payment link for PayOS
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/payos/create-payment-link
    ///     {
    ///         "amount": 10000,
    ///         "description": "Payment for order #123",
    ///         "returnUrl": "https://example.com/return",
    ///         "cancelUrl": "https://example.com/cancel",
    ///         "orderCode": null
    ///     }
    /// </remarks>
    /// <param name="request">Payment link creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment link details with checkout URL</returns>
    [HttpPost("create-payment-link")]
    [ProducesResponseType(typeof(Result<CreatePaymentLinkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePaymentLink(
        [FromBody] CreatePaymentLinkRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (request == null)
            {
                _logger.LogWarning("CreatePaymentLink: Request is null");
                return BadRequest(Result<CreatePaymentLinkResponse>.Failure("Request cannot be empty", "INVALID_REQUEST"));
            }

            if (request.Amount <= 0)
            {
                _logger.LogWarning("CreatePaymentLink: Invalid amount {Amount}", request.Amount);
                return BadRequest(Result<CreatePaymentLinkResponse>.Failure("Amount must be greater than 0", "INVALID_AMOUNT"));
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                _logger.LogWarning("CreatePaymentLink: Description is empty");
                return BadRequest(Result<CreatePaymentLinkResponse>.Failure("Description is required", "MISSING_DESCRIPTION"));
            }

            if (string.IsNullOrWhiteSpace(request.ReturnUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
            {
                _logger.LogWarning("CreatePaymentLink: Missing return or cancel URL");
                return BadRequest(Result<CreatePaymentLinkResponse>.Failure("Return URL and Cancel URL are required", "MISSING_URL"));
            }

            _logger.LogInformation("Creating payment link for amount: {Amount}, description: {Description}", 
                request.Amount, request.Description);

            var result = await _payOSService.CreatePayOSPaymentLinkAsync(request, cancellationToken);

            _logger.LogInformation("Payment link created successfully with order code: {OrderCode}", result.OrderCode);

            return Ok(Result<CreatePaymentLinkResponse>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when creating payment link");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Result<CreatePaymentLinkResponse>.Failure(ex.Message, "PAYMENT_LINK_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating payment link");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Result<CreatePaymentLinkResponse>.Failure("An unexpected error occurred while creating the payment link", InternalServerError));
        }
    }

    /// <summary>
    /// Get payment link information from PayOS
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/payos/payment-link/124c33293c934a85be5b7f8761a27a07
    /// 
    /// Returns payment status, amounts, and transaction details
    /// </remarks>
    /// <param name="paymentLinkId">Payment request ID from PayOS</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment link information including status and transactions</returns>
    [HttpGet("payment-link/{paymentLinkId}")]
    [ProducesResponseType(typeof(Result<GetPaymentLinkInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentLinkInfo(
        [FromRoute] string paymentLinkId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate payment link ID
            if (string.IsNullOrWhiteSpace(paymentLinkId))
            {
                _logger.LogWarning("GetPaymentLinkInfo: Payment link ID is empty");
                return BadRequest(Result<GetPaymentLinkInfoResponse>.Failure("Payment link ID is required", "MISSING_PAYMENT_LINK_ID"));
            }

            _logger.LogInformation("Fetching payment link info for ID: {PaymentLinkId}", paymentLinkId);

            var result = await _payOSService.GetPaymentLinkInfoAsync(paymentLinkId, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("GetPaymentLinkInfo: Payment link not found for ID: {PaymentLinkId}", paymentLinkId);
                return NotFound(Result<GetPaymentLinkInfoResponse>.Failure("Payment link not found", "PAYMENT_LINK_NOT_FOUND"));
            }

            _logger.LogInformation("Payment link info retrieved successfully for ID: {PaymentLinkId}, Status: {Status}", 
                paymentLinkId, result.Status);

            return Ok(Result<GetPaymentLinkInfoResponse>.Success(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when getting payment link info");
            return BadRequest(Result<GetPaymentLinkInfoResponse>.Failure(ex.Message, "INVALID_ARGUMENT"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when getting payment link info");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Result<GetPaymentLinkInfoResponse>.Failure(ex.Message, "PAYMENT_LINK_INFO_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting payment link info");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Result<GetPaymentLinkInfoResponse>.Failure("An unexpected error occurred while retrieving payment link information", InternalServerError));
        }
    }

    /// <summary>
    /// Cancel a payment link on PayOS
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/payos/payment-link/124c33293c934a85be5b7f8761a27a07/cancel
    /// 
    /// Returns the cancelled payment link information with status CANCELLED and cancellation details
    /// </remarks>
    /// <param name="paymentLinkId">Payment request ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancelled payment link information</returns>
    [HttpPost("payment-link/{paymentLinkId}/cancel")]
    [ProducesResponseType(typeof(Result<CancelPaymentLinkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelPaymentLink(
        [FromRoute] string paymentLinkId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate payment link ID
            if (string.IsNullOrWhiteSpace(paymentLinkId))
            {
                _logger.LogWarning("CancelPaymentLink: Payment link ID is empty");
                return BadRequest(Result<CancelPaymentLinkResponse>.Failure("Payment link ID is required", "MISSING_PAYMENT_LINK_ID"));
            }

            _logger.LogInformation("Cancelling payment link for ID: {PaymentLinkId}", paymentLinkId);

            var result = await _payOSService.CancelPaymentLinkAsync(paymentLinkId, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("CancelPaymentLink: Payment link not found for ID: {PaymentLinkId}", paymentLinkId);
                return NotFound(Result<CancelPaymentLinkResponse>.Failure("Payment link not found", "PAYMENT_LINK_NOT_FOUND"));
            }

            _logger.LogInformation("Payment link cancelled successfully for ID: {PaymentLinkId}, Status: {Status}", 
                paymentLinkId, result.Status);

            return Ok(Result<CancelPaymentLinkResponse>.Success(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when cancelling payment link");
            return BadRequest(Result<CancelPaymentLinkResponse>.Failure(ex.Message, "INVALID_ARGUMENT"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when cancelling payment link");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Result<CancelPaymentLinkResponse>.Failure(ex.Message, "CANCEL_PAYMENT_LINK_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error cancelling payment link");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Result<CancelPaymentLinkResponse>.Failure("An unexpected error occurred while cancelling the payment link", InternalServerError));
        }
    }

    /// <summary>
    /// Get payment invoices from PayOS
    /// </summary>
    /// <remarks>
    /// Retrieves all invoices associated with a payment request from PayOS.
    /// Sample request:
    /// 
    ///     GET /api/payos/payment-link/124c33293c934a85be5b7f8761a27a07/invoices
    /// 
    /// Returns invoice details including invoice ID, number, dates, and transaction information
    /// </remarks>
    /// <param name="paymentLinkId">Payment request ID from PayOS</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payment invoices</returns>
    [HttpGet("payment-link/{paymentLinkId}/invoices")]
    [ProducesResponseType(typeof(Result<GetPaymentInvoicesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentInvoices(
        [FromRoute] string paymentLinkId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate payment link ID
            if (string.IsNullOrWhiteSpace(paymentLinkId))
            {
                _logger.LogWarning("GetPaymentInvoices: Payment link ID is empty");
                return BadRequest(Result<GetPaymentInvoicesResponse>.Failure("Payment link ID is required", "MISSING_PAYMENT_LINK_ID"));
            }

            _logger.LogInformation("Fetching payment invoices for ID: {PaymentLinkId}", paymentLinkId);

            var result = await _payOSService.GetPaymentInvoicesAsync(paymentLinkId, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("GetPaymentInvoices: No invoices found for ID: {PaymentLinkId}", paymentLinkId);
                return NotFound(Result<GetPaymentInvoicesResponse>.Failure("No invoices found", "INVOICES_NOT_FOUND"));
            }

            _logger.LogInformation("Payment invoices retrieved successfully for ID: {PaymentLinkId}, Invoice Count: {InvoiceCount}",
                paymentLinkId, result.Invoices?.Count ?? 0);

            return Ok(Result<GetPaymentInvoicesResponse>.Success(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when getting payment invoices");
            return BadRequest(Result<GetPaymentInvoicesResponse>.Failure(ex.Message, "INVALID_ARGUMENT"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when getting payment invoices");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<GetPaymentInvoicesResponse>.Failure(ex.Message, "PAYMENT_INVOICES_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting payment invoices");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<GetPaymentInvoicesResponse>.Failure("An unexpected error occurred while retrieving payment invoices", InternalServerError));
        }
    }
    /// <summary>
    /// Webhook endpoint for PayOS payment notifications
    /// </summary>
    /// <remarks>
    /// Receives payment confirmation from PayOS and updates:
    /// - PaymentTransaction status (Completed/Failed)
    /// - Order status (Paid)
    /// - SchoolWallet balance (add payment amount)
    /// - Product variants inventory
    /// 
    /// Only processes if:
    /// 1. Payment webhook success = true
    /// 2. Transaction is not already in final state (Completed/Cancelled)
    /// </remarks>
    /// <param name="webhook">Payment webhook response from PayOS</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<PaymentWebhookProcessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PaymentWebhook(
        [FromBody] PaymentWebhookResponse webhook,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("payment webhook infor: " + JsonConvert.SerializeObject(webhook));
            _logger.LogInformation("Received PayOS webhook for PaymentLinkId: {PaymentLinkId}, Success: {Success}",
                webhook?.Data?.PaymentLinkId, webhook?.Success);
            if (webhook != null)
            {
                if(webhook?.Data?.AccountNumber == "12345678")
                {
                    return Ok("ok");
                }
            }
            var result = await _paymentWebhookHandler.HandleWebhookAsync(webhook, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Webhook processing failed: {Error}", result.Error);
                return BadRequest(result);
            }

            _logger.LogInformation("Webhook processed successfully: {Message}", result.Value?.Message);
            return Ok(result);
            //return Ok("OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment webhook");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Result<PaymentWebhookProcessResponse>.Failure(
                    "An unexpected error occurred while processing the webhook",
                    "INTERNAL_SERVER_ERROR"));
        }
    }

    #region Payout Endpoints

    /// <summary>
    /// Get payout account balance from PayOS
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/payos/payout/account-detail
    /// 
    /// Returns the current payout account balance and currency
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout account balance detail</returns>
    [HttpGet("payout/account-detail")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<PayoutAccountDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPayoutAccountDetail(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching payout account detail");

            var result = await _payOSService.GetPayoutAccountDetailAsync(cancellationToken);

            return Ok(Result<PayoutAccountDetailResponse>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when fetching payout account detail");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<PayoutAccountDetailResponse>.Failure(ex.Message, "PAYOUT_ACCOUNT_DETAIL_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching payout account detail");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<PayoutAccountDetailResponse>.Failure("An unexpected error occurred while fetching payout account detail", InternalServerError));
        }
    }

    /// <summary>
    /// Get list of payouts from PayOS with optional filters
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/payos/payout/list?limit=10&amp;offset=0&amp;approvalState=PROCESSING
    /// 
    /// Supports filtering by referenceId, approvalState, category, fromDate, toDate
    /// </remarks>
    /// <param name="query">Query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated payout list</returns>
    [HttpGet("payout/list")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<PayoutListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPayoutList(
        [FromQuery] PayoutListQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching payout list with Limit={Limit}, Offset={Offset}", query?.Limit, query?.Offset);

            var result = await _payOSService.GetPayoutListAsync(query, cancellationToken);

            return Ok(Result<PayoutListResponse>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when fetching payout list");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<PayoutListResponse>.Failure(ex.Message, "PAYOUT_LIST_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching payout list");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<PayoutListResponse>.Failure("An unexpected error occurred while fetching payout list", InternalServerError));
        }
    }

    /// <summary>
    /// Get payout detail by payout ID from PayOS
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/payos/payout/payout_123456789
    /// 
    /// Returns payout detail including transactions, category, and approval state
    /// </remarks>
    /// <param name="payoutId">Payout ID from PayOS</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout detail including transactions</returns>
    [HttpGet("payout/{payoutId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<PayoutDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPayoutDetail(
        [FromRoute] string payoutId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payoutId))
            {
                return BadRequest(Result<PayoutDetailResponse>.Failure("Payout ID is required", "MISSING_PAYOUT_ID"));
            }

            _logger.LogInformation("Fetching payout detail for ID: {PayoutId}", payoutId);

            var result = await _payOSService.GetPayoutDetailAsync(payoutId, cancellationToken);

            return Ok(Result<PayoutDetailResponse>.Success(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when fetching payout detail");
            return BadRequest(Result<PayoutDetailResponse>.Failure(ex.Message, "INVALID_ARGUMENT"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when fetching payout detail");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<PayoutDetailResponse>.Failure(ex.Message, "PAYOUT_DETAIL_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching payout detail");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<PayoutDetailResponse>.Failure("An unexpected error occurred while fetching payout detail", InternalServerError));
        }
    }

    /// <summary>
    /// Create a new payout (disbursement) on PayOS
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/payos/payout/create
    ///     {
    ///         "referenceId": "payout_123",
    ///         "amount": 100000,
    ///         "description": "Thanh toán lương",
    ///         "toBin": "970415",
    ///         "toAccountNumber": "123456789",
    ///         "category": ["salary"]
    ///     }
    /// 
    /// Returns the created payout with transactions and approval state
    /// </remarks>
    /// <param name="request">Payout creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payout response</returns>
    [HttpPost("payout/create")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<CreatePayoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePayout(
        [FromBody] CreatePayoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(Result<CreatePayoutResponse>.Failure("Request cannot be empty", "INVALID_REQUEST"));
            }

            if (string.IsNullOrWhiteSpace(request.ReferenceId))
            {
                return BadRequest(Result<CreatePayoutResponse>.Failure("ReferenceId is required", "MISSING_REFERENCE_ID"));
            }

            if (request.Amount <= 0)
            {
                return BadRequest(Result<CreatePayoutResponse>.Failure("Amount must be greater than 0", "INVALID_AMOUNT"));
            }

            if (string.IsNullOrWhiteSpace(request.ToBin))
            {
                return BadRequest(Result<CreatePayoutResponse>.Failure("ToBin (bank code) is required", "MISSING_TO_BIN"));
            }

            if (string.IsNullOrWhiteSpace(request.ToAccountNumber))
            {
                return BadRequest(Result<CreatePayoutResponse>.Failure("ToAccountNumber is required", "MISSING_TO_ACCOUNT_NUMBER"));
            }

            _logger.LogInformation("Creating payout. ReferenceId: {ReferenceId}, Amount: {Amount}, ToBin: {ToBin}",
                request.ReferenceId, request.Amount, request.ToBin);

            var result = await _payOSService.CreatePayoutAsync(request, cancellationToken);

            _logger.LogInformation("Payout created successfully. Id: {Id}, State: {State}",
                result.Id, result.ApprovalState);

            return Ok(Result<CreatePayoutResponse>.Success(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating payout");
            return BadRequest(Result<CreatePayoutResponse>.Failure(ex.Message, "INVALID_ARGUMENT"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when creating payout");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<CreatePayoutResponse>.Failure(ex.Message, "CREATE_PAYOUT_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating payout");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<CreatePayoutResponse>.Failure("An unexpected error occurred while creating payout", InternalServerError));
        }
    }

    #endregion
}
