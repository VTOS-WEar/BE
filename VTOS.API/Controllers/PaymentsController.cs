using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Payments.Commands;
using VTOS.Application.Features.Payments.Queries;
using VTOS.Application.Features.Users.Commands;

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
    private readonly IGetParentWalletQueryHandler _getParentWalletHandler;
    private readonly IGetParentWalletTransactionsQueryHandler _getParentWalletTransactionsHandler;
    private readonly ICreateParentWithdrawalRequestCommandHandler _createParentWithdrawalHandler;
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
        IGetParentWalletQueryHandler getParentWalletHandler,
        IGetParentWalletTransactionsQueryHandler getParentWalletTransactionsHandler,
        ICreateParentWithdrawalRequestCommandHandler createParentWithdrawalHandler,
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
        _getParentWalletHandler = getParentWalletHandler;
        _getParentWalletTransactionsHandler = getParentWalletTransactionsHandler;
        _createParentWithdrawalHandler = createParentWithdrawalHandler;
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

    /// <summary>Get parent wallet info for refunds and withdrawals.</summary>
    [HttpGet("parent/wallet")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParentWallet(CancellationToken ct)
    {
        var result = await _getParentWalletHandler.HandleAsync(new GetParentWalletQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get parent wallet transaction history.</summary>
    [HttpGet("parent/wallet/transactions")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParentWalletTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _getParentWalletTransactionsHandler.HandleAsync(
            new GetParentWalletTransactionsQuery(_currentUser.UserId, page, pageSize), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Create a parent wallet withdrawal request.</summary>
    [HttpPost("parent/wallet/withdrawals")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateParentWithdrawalRequest([FromBody] CreateParentWithdrawalRequest request, CancellationToken ct)
    {
        var result = await _createParentWithdrawalHandler.HandleAsync(
            new CreateParentWithdrawalRequestCommand(_currentUser.UserId, request.Amount), ct);
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
public record CreateParentWithdrawalRequest(decimal Amount);
