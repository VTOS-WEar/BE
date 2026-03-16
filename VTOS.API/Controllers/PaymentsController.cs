using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Payments.Commands;
using VTOS.Application.Features.Payments.Queries;

namespace VTOS.API.Controllers;

/// <summary>
/// Phase 6 — Internal Payment APIs.
/// Handles Parent order payments, School wallet management, Provider revenue.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPayOrderCommandHandler _payOrderHandler;
    private readonly IPayProviderCommandHandler _payProviderHandler;
    private readonly IRefundOrderCommandHandler _refundOrderHandler;
    private readonly IUpdateWalletBankInfoCommandHandler _updateBankInfoHandler;
    private readonly IGenerateInvoiceCommandHandler _generateInvoiceHandler;
    private readonly IGetSchoolWalletQueryHandler _getWalletHandler;
    private readonly IGetWalletTransactionsQueryHandler _getTransactionsHandler;
    private readonly IGetParentPaymentHistoryQueryHandler _getParentPaymentsHandler;
    private readonly IGetProviderRevenueQueryHandler _getRevenueHandler;
    private readonly IGetProviderPaymentHistoryQueryHandler _getProviderPaymentsHandler;

    public PaymentsController(
        ICurrentUserService currentUser,
        IPayOrderCommandHandler payOrderHandler,
        IPayProviderCommandHandler payProviderHandler,
        IRefundOrderCommandHandler refundOrderHandler,
        IUpdateWalletBankInfoCommandHandler updateBankInfoHandler,
        IGenerateInvoiceCommandHandler generateInvoiceHandler,
        IGetSchoolWalletQueryHandler getWalletHandler,
        IGetWalletTransactionsQueryHandler getTransactionsHandler,
        IGetParentPaymentHistoryQueryHandler getParentPaymentsHandler,
        IGetProviderRevenueQueryHandler getRevenueHandler,
        IGetProviderPaymentHistoryQueryHandler getProviderPaymentsHandler)
    {
        _currentUser = currentUser;
        _payOrderHandler = payOrderHandler;
        _payProviderHandler = payProviderHandler;
        _refundOrderHandler = refundOrderHandler;
        _updateBankInfoHandler = updateBankInfoHandler;
        _generateInvoiceHandler = generateInvoiceHandler;
        _getWalletHandler = getWalletHandler;
        _getTransactionsHandler = getTransactionsHandler;
        _getParentPaymentsHandler = getParentPaymentsHandler;
        _getRevenueHandler = getRevenueHandler;
        _getProviderPaymentsHandler = getProviderPaymentsHandler;
    }

    // ═══════════════════════════════════════════════════════════════
    //  PARENT — Pay Order
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Parent pays for an order. Money goes to SchoolWallet automatically.</summary>
    [HttpPost("orders/{orderId:guid}/pay")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PayOrder(Guid orderId, CancellationToken ct)
    {
        var result = await _payOrderHandler.HandleAsync(new PayOrderCommand(_currentUser.UserId, orderId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Parent views their payment history.</summary>
    [HttpGet("parent/history")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParentPaymentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _getParentPaymentsHandler.HandleAsync(new GetParentPaymentHistoryQuery(_currentUser.UserId, page, pageSize), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════
    //  SCHOOL — Wallet Management
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Get school wallet info (balance, bank info).</summary>
    [HttpGet("school/wallet")]
    [Authorize(Roles = "School")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchoolWallet(CancellationToken ct)
    {
        var result = await _getWalletHandler.HandleAsync(new GetSchoolWalletQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get school wallet transaction history.</summary>
    [HttpGet("school/wallet/transactions")]
    [Authorize(Roles = "School")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWalletTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _getTransactionsHandler.HandleAsync(new GetWalletTransactionsQuery(_currentUser.UserId, page, pageSize), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Update school wallet bank information.</summary>
    [HttpPut("school/wallet/bank-info")]
    [Authorize(Roles = "School")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateWalletBankInfo([FromBody] UpdateBankInfoRequest request, CancellationToken ct)
    {
        var result = await _updateBankInfoHandler.HandleAsync(
            new UpdateWalletBankInfoCommand(_currentUser.UserId, request.BankCode, request.BankName, request.AccountNumber, request.AccountName), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════
    //  SCHOOL — Pay Provider & Refund
    // ═══════════════════════════════════════════════════════════════

    /// <summary>School pays provider for a delivered order from wallet.</summary>
    [HttpPost("school/orders/{orderId:guid}/pay-provider")]
    [Authorize(Roles = "School")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PayProvider(Guid orderId, CancellationToken ct)
    {
        var result = await _payProviderHandler.HandleAsync(new PayProviderCommand(_currentUser.UserId, orderId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>School initiates refund for an order.</summary>
    [HttpPost("school/orders/{orderId:guid}/refund")]
    [Authorize(Roles = "School")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefundOrder(Guid orderId, [FromBody] RefundRequest? request, CancellationToken ct)
    {
        var result = await _refundOrderHandler.HandleAsync(
            new RefundOrderCommand(_currentUser.UserId, orderId, request?.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PROVIDER — Revenue & Invoices
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Provider revenue dashboard (total revenue, paid/pending orders).</summary>
    [HttpGet("provider/revenue")]
    [Authorize(Roles = "Provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderRevenue(CancellationToken ct)
    {
        var result = await _getRevenueHandler.HandleAsync(new GetProviderRevenueQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Provider payment history (money received from schools).</summary>
    [HttpGet("provider/payments")]
    [Authorize(Roles = "Provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderPaymentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _getProviderPaymentsHandler.HandleAsync(new GetProviderPaymentHistoryQuery(_currentUser.UserId, page, pageSize), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Provider generates an invoice for an order.</summary>
    [HttpPost("provider/orders/{orderId:guid}/invoice")]
    [Authorize(Roles = "Provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateInvoice(Guid orderId, CancellationToken ct)
    {
        var result = await _generateInvoiceHandler.HandleAsync(new GenerateInvoiceCommand(_currentUser.UserId, orderId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }
}

/// <summary>Request body for updating wallet bank info.</summary>
public record UpdateBankInfoRequest(string BankCode, string BankName, string AccountNumber, string AccountName);

/// <summary>Request body for refund.</summary>
public record RefundRequest(string? Reason);
