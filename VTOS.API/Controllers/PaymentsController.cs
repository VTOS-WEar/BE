using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Payments.Commands;
using VTOS.Application.Features.Payments.Queries;

namespace VTOS.API.Controllers;

/// <summary>
/// Phase 6 - Internal Payment APIs.
/// Handles parent order payments and provider revenue operations.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPayOrderCommandHandler _payOrderHandler;
    private readonly IGenerateInvoiceCommandHandler _generateInvoiceHandler;
    private readonly IGetParentPaymentHistoryQueryHandler _getParentPaymentsHandler;
    private readonly IGetProviderRevenueQueryHandler _getRevenueHandler;
    private readonly IGetProviderPaymentHistoryQueryHandler _getProviderPaymentsHandler;
    private readonly IGetProviderWalletQueryHandler _getProviderWalletHandler;
    private readonly IGetProviderWalletTransactionsQueryHandler _getProviderWalletTransactionsHandler;
    private readonly IUpdateWalletBankInfoCommandHandler _updateBankInfoHandler;

    public PaymentsController(
        ICurrentUserService currentUser,
        IPayOrderCommandHandler payOrderHandler,
        IGenerateInvoiceCommandHandler generateInvoiceHandler,
        IGetParentPaymentHistoryQueryHandler getParentPaymentsHandler,
        IGetProviderRevenueQueryHandler getRevenueHandler,
        IGetProviderPaymentHistoryQueryHandler getProviderPaymentsHandler,
        IGetProviderWalletQueryHandler getProviderWalletHandler,
        IGetProviderWalletTransactionsQueryHandler getProviderWalletTransactionsHandler,
        IUpdateWalletBankInfoCommandHandler updateBankInfoHandler)
    {
        _currentUser = currentUser;
        _payOrderHandler = payOrderHandler;
        _generateInvoiceHandler = generateInvoiceHandler;
        _getParentPaymentsHandler = getParentPaymentsHandler;
        _getRevenueHandler = getRevenueHandler;
        _getProviderPaymentsHandler = getProviderPaymentsHandler;
        _getProviderWalletHandler = getProviderWalletHandler;
        _getProviderWalletTransactionsHandler = getProviderWalletTransactionsHandler;
        _updateBankInfoHandler = updateBankInfoHandler;
    }

    /// <summary>Parent pays for an order.</summary>
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
    public async Task<IActionResult> GetParentPaymentHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getParentPaymentsHandler.HandleAsync(
            new GetParentPaymentHistoryQuery(_currentUser.UserId, page, pageSize, startDate, endDate, status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get provider wallet info (balance, bank info).</summary>
    [HttpGet("provider/wallet")]
    [Authorize(Roles = "Provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderWallet(CancellationToken ct)
    {
        var result = await _getProviderWalletHandler.HandleAsync(new GetProviderWalletQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get provider wallet transaction history.</summary>
    [HttpGet("provider/wallet/transactions")]
    [Authorize(Roles = "Provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderWalletTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _getProviderWalletTransactionsHandler.HandleAsync(
            new GetProviderWalletTransactionsQuery(_currentUser.UserId, page, pageSize), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Update provider wallet bank information.</summary>
    [HttpPut("provider/wallet/bank-info")]
    [Authorize(Roles = "Provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProviderWalletBankInfo([FromBody] UpdateBankInfoRequest request, CancellationToken ct)
    {
        var result = await _updateBankInfoHandler.HandleAsync(
            new UpdateWalletBankInfoCommand(_currentUser.UserId, request.BankCode, request.BankName, request.AccountNumber, request.AccountName), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

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
