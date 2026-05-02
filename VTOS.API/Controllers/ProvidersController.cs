using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Contracts.Commands;
using VTOS.Application.Features.Contracts.DTOs;
using VTOS.Application.Features.Contracts.Queries;
using VTOS.Application.Features.Providers.Commands;
using VTOS.Application.Features.Providers.DTOs;
using VTOS.Application.Features.Providers.Queries;

namespace VTOS.API.Controllers;

/// <summary>
/// Provider management APIs (requires Provider role).
/// UC-48: Provider Dashboard &amp; Profile
/// Phase 2: Contract review + approve/reject
/// Phase 3: Production Order management
/// </summary>
[ApiController]
[Route("api/providers")]
[Authorize(Roles = "Provider")]
public class ProvidersController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetProviderProfileQueryHandler _getProfileHandler;
    private readonly IUpdateProviderProfileCommandHandler _updateProfileHandler;
    private readonly IGetContractsQueryHandler _getContractsHandler;
    private readonly IGetContractDetailQueryHandler _getContractDetailHandler;
    private readonly IUpdateContractPricingCommandHandler _updateContractPricingHandler;
    private readonly IApproveContractCommandHandler _approveContractHandler;
    private readonly IRejectContractCommandHandler _rejectContractHandler;
    private readonly IRequestSignOTPCommandHandler _requestSignOTPHandler;
    private readonly ISignContractByProviderCommandHandler _signContractByProviderHandler;
    // Phase 5 — Complaints
    private readonly IGetProviderSupportTicketsQueryHandler _getProviderComplaintsHandler;
    private readonly IGetProviderSupportTicketDetailQueryHandler _getProviderComplaintDetailHandler;
    private readonly IRespondSupportTicketCommandHandler _respondComplaintHandler;
    // Withdrawal
    private readonly ICreateProviderWithdrawalRequestCommandHandler _createWithdrawalHandler;
    private readonly IGetProviderWithdrawalRequestsQueryHandler _getWithdrawalRequestsHandler;
    private readonly IGetProviderIncomingOrdersQueryHandler _getProviderIncomingOrdersHandler;
    private readonly IGetProviderDirectOrderDetailQueryHandler _getProviderDirectOrderDetailHandler;
    private readonly IAcceptDirectOrderCommandHandler _acceptDirectOrderHandler;
    private readonly IUpdateDirectOrderInProductionCommandHandler _updateDirectOrderInProductionHandler;
    private readonly IMarkDirectOrderReadyToShipCommandHandler _markDirectOrderReadyToShipHandler;
    private readonly IShipDirectOrderCommandHandler _shipDirectOrderHandler;
    private readonly IGetProviderOrderStatsQueryHandler _getProviderOrderStatsHandler;
    private readonly IGetProviderCatalogQueryHandler _getProviderCatalogHandler;
    private readonly IUpsertProviderCatalogItemCommandHandler _upsertProviderCatalogItemHandler;

    public ProvidersController(
        ICurrentUserService currentUser,
        IGetProviderProfileQueryHandler getProfileHandler,
        IUpdateProviderProfileCommandHandler updateProfileHandler,
        IGetContractsQueryHandler getContractsHandler,
        IGetContractDetailQueryHandler getContractDetailHandler,
        IUpdateContractPricingCommandHandler updateContractPricingHandler,
        IApproveContractCommandHandler approveContractHandler,
        IRejectContractCommandHandler rejectContractHandler,
        IRequestSignOTPCommandHandler requestSignOTPHandler,
        ISignContractByProviderCommandHandler signContractByProviderHandler,
        // Phase 5
        IGetProviderSupportTicketsQueryHandler getProviderComplaintsHandler,
        IGetProviderSupportTicketDetailQueryHandler getProviderComplaintDetailHandler,
        IRespondSupportTicketCommandHandler respondComplaintHandler,
        ICreateProviderWithdrawalRequestCommandHandler createWithdrawalHandler,
        IGetProviderWithdrawalRequestsQueryHandler getWithdrawalRequestsHandler,
        IGetProviderIncomingOrdersQueryHandler getProviderIncomingOrdersHandler,
        IGetProviderDirectOrderDetailQueryHandler getProviderDirectOrderDetailHandler,
        IAcceptDirectOrderCommandHandler acceptDirectOrderHandler,
        IUpdateDirectOrderInProductionCommandHandler updateDirectOrderInProductionHandler,
        IMarkDirectOrderReadyToShipCommandHandler markDirectOrderReadyToShipHandler,
        IShipDirectOrderCommandHandler shipDirectOrderHandler,
        IGetProviderOrderStatsQueryHandler getProviderOrderStatsHandler,
        IGetProviderCatalogQueryHandler getProviderCatalogHandler,
        IUpsertProviderCatalogItemCommandHandler upsertProviderCatalogItemHandler)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _getContractsHandler = getContractsHandler;
        _getContractDetailHandler = getContractDetailHandler;
        _updateContractPricingHandler = updateContractPricingHandler;
        _approveContractHandler = approveContractHandler;
        _rejectContractHandler = rejectContractHandler;
        _requestSignOTPHandler = requestSignOTPHandler;
        _signContractByProviderHandler = signContractByProviderHandler;
        _getProviderComplaintsHandler = getProviderComplaintsHandler;
        _getProviderComplaintDetailHandler = getProviderComplaintDetailHandler;
        _respondComplaintHandler = respondComplaintHandler;
        _createWithdrawalHandler = createWithdrawalHandler;
        _getWithdrawalRequestsHandler = getWithdrawalRequestsHandler;
        _getProviderIncomingOrdersHandler = getProviderIncomingOrdersHandler;
        _getProviderDirectOrderDetailHandler = getProviderDirectOrderDetailHandler;
        _acceptDirectOrderHandler = acceptDirectOrderHandler;
        _updateDirectOrderInProductionHandler = updateDirectOrderInProductionHandler;
        _markDirectOrderReadyToShipHandler = markDirectOrderReadyToShipHandler;
        _shipDirectOrderHandler = shipDirectOrderHandler;
        _getProviderOrderStatsHandler = getProviderOrderStatsHandler;
        _getProviderCatalogHandler = getProviderCatalogHandler;
        _upsertProviderCatalogItemHandler = upsertProviderCatalogItemHandler;
    }

    // ──────── Profile ────────

    /// <summary>Get current provider's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProviderProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await _getProfileHandler.HandleAsync(
            new GetProviderProfileQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Update current provider's profile (partial update).</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ProviderProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProviderProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateProviderProfileCommand(
            _currentUser.UserId,
            request.ProviderName,
            request.ContactPersonName,
            request.Phone,
            request.Email,
            request.Address
        );

        var result = await _updateProfileHandler.HandleAsync(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ──────── Catalog Management ────────

    /// <summary>List provider-managed catalog items for approved semester publications.</summary>
    [HttpGet("me/catalog")]
    [ProducesResponseType(typeof(ProviderCatalogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _getProviderCatalogHandler.HandleAsync(
            new GetProviderCatalogQuery(_currentUser.UserId, page, pageSize, status, search), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Create or update provider-owned catalog data for one outfit in a semester publication.</summary>
    [HttpPut("me/catalog/{semesterPublicationProviderId:guid}/items/{outfitId:guid}")]
    [ProducesResponseType(typeof(ProviderCatalogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertCatalogItem(
        Guid semesterPublicationProviderId,
        Guid outfitId,
        [FromBody] UpsertProviderCatalogItemRequest request,
        CancellationToken ct)
    {
        var result = await _upsertProviderCatalogItemHandler.HandleAsync(
            new UpsertProviderCatalogItemCommand(_currentUser.UserId, semesterPublicationProviderId, outfitId, request), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ──────── Contract Management (Phase 2) ────────

    /// <summary>List contracts sent to this provider, with optional status filter.</summary>
    [HttpGet("me/contracts")]
    [ProducesResponseType(typeof(ContractListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContracts(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _getContractsHandler.HandleAsync(
            new GetContractsQuery(_currentUser.UserId, "Provider", status, page, pageSize, search), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get contract detail by ID (provider-scoped).</summary>
    [HttpGet("me/contracts/{id}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractDetail(Guid id, CancellationToken ct)
    {
        var result = await _getContractDetailHandler.HandleAsync(
            new GetContractDetailQuery(_currentUser.UserId, "Provider", id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Update provider-owned pricing for a pending contract.</summary>
    [HttpPut("me/contracts/{id}/pricing")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateContractPricing(Guid id, [FromBody] UpdateContractPricingRequest request, CancellationToken ct)
    {
        var result = await _updateContractPricingHandler.HandleAsync(
            new UpdateContractPricingCommand(_currentUser.UserId, id, request), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Approve a pending contract.</summary>
    [HttpPut("me/contracts/{id}/approve")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveContract(Guid id, CancellationToken ct)
    {
        var result = await _approveContractHandler.HandleAsync(
            new ApproveContractCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Reject a pending contract with a reason.</summary>
    [HttpPut("me/contracts/{id}/reject")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectContract(Guid id, [FromBody] RejectContractRequest request, CancellationToken ct)
    {
        var result = await _rejectContractHandler.HandleAsync(
            new RejectContractCommand(_currentUser.UserId, id, request.Reason), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Request a 6-digit OTP to be sent to the provider's email for signing.</summary>
    [HttpPost("me/contracts/{id}/request-sign-otp")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestSignOTP(Guid id, CancellationToken ct)
    {
        var result = await _requestSignOTPHandler.HandleAsync(
            new RequestSignOTPCommand(_currentUser.UserId, id, "Provider"), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { success = result.Value });
    }

    /// <summary>Sign the contract using OTP + base64 signature image (PendingProviderSign → Active).</summary>
    [HttpPut("me/contracts/{id}/sign")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignContract(Guid id, [FromBody] SignContractRequest request, CancellationToken ct)
    {
        var result = await _signContractByProviderHandler.HandleAsync(
            new SignContractByProviderCommand(_currentUser.UserId, id, request), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ──────── SupportTicket Management (Phase 5) ────────

    /// <summary>List complaints sent to this provider.</summary>
    [HttpGet("me/complaints")]
    [ProducesResponseType(typeof(GetProviderSupportTicketsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderComplaints(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getProviderComplaintsHandler.HandleAsync(
            new GetProviderSupportTicketsQuery(_currentUser.UserId, page, pageSize, status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get complaint detail by ID (provider-scoped).</summary>
    [HttpGet("me/complaints/{id:guid}")]
    [ProducesResponseType(typeof(Application.Features.Schools.Queries.SupportTicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderComplaintDetail(Guid id, CancellationToken ct)
    {
        var result = await _getProviderComplaintDetailHandler.HandleAsync(
            new GetProviderSupportTicketDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Respond to a complaint (optionally mark as resolved).</summary>
    [HttpPut("me/complaints/{id:guid}/respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondComplaint(Guid id, [FromBody] RespondComplaintRequest request, CancellationToken ct)
    {
        var result = await _respondComplaintHandler.HandleAsync(
            new RespondSupportTicketCommand(_currentUser.UserId, id, request.Response, request.MarkResolved), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    // ──────── Wallet Withdrawal ────────

    /// <summary>Create a withdrawal request from provider wallet.</summary>
    [HttpPost("me/wallet/withdrawals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWithdrawalRequest(
        [FromBody] CreateProviderWithdrawalRequest request,
        CancellationToken ct)
    {
        var result = await _createWithdrawalHandler.HandleAsync(
            new CreateProviderWithdrawalRequestCommand(_currentUser.UserId, request.Amount), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>List withdrawal requests for the current provider wallet.</summary>
    [HttpGet("me/wallet/withdrawals")]
    [ProducesResponseType(typeof(ProviderWithdrawalRequestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWithdrawalRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getWithdrawalRequestsHandler.HandleAsync(
            new GetProviderWithdrawalRequestsQuery(_currentUser.UserId, page, pageSize, status), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpGet("me/orders")]
    [ProducesResponseType(typeof(ProviderIncomingOrdersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncomingOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _getProviderIncomingOrdersHandler.HandleAsync(
            new GetProviderIncomingOrdersQuery(_currentUser.UserId, page, pageSize, status, fromDate, toDate, search), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpGet("me/orders/{id:guid}")]
    [ProducesResponseType(typeof(ProviderDirectOrderDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncomingOrderDetail(Guid id, CancellationToken ct = default)
    {
        var result = await _getProviderDirectOrderDetailHandler.HandleAsync(
            new GetProviderDirectOrderDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "ORDER_NOT_FOUND"
                ? NotFound(new { error = result.Error, code = result.ErrorCode })
                : BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpPut("me/orders/{id:guid}/accept")]
    public async Task<IActionResult> AcceptDirectOrder(Guid id, CancellationToken ct = default)
    {
        var result = await _acceptDirectOrderHandler.HandleAsync(new AcceptDirectOrderCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = "Order accepted successfully." });
    }

    [HttpPut("me/orders/{id:guid}/in-production")]
    public async Task<IActionResult> UpdateDirectOrderInProduction(Guid id, CancellationToken ct = default)
    {
        var result = await _updateDirectOrderInProductionHandler.HandleAsync(new UpdateDirectOrderInProductionCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = "Order moved to in-production." });
    }

    [HttpPut("me/orders/{id:guid}/ready-to-ship")]
    public async Task<IActionResult> MarkDirectOrderReadyToShip(Guid id, CancellationToken ct = default)
    {
        var result = await _markDirectOrderReadyToShipHandler.HandleAsync(new MarkDirectOrderReadyToShipCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = "Order marked ready to ship." });
    }

    [HttpPut("me/orders/{id:guid}/ship")]
    public async Task<IActionResult> ShipDirectOrder(Guid id, [FromBody] ShipDirectOrderRequest request, CancellationToken ct = default)
    {
        var result = await _shipDirectOrderHandler.HandleAsync(
            new ShipDirectOrderCommand(_currentUser.UserId, id, request.TrackingCode, request.ShippingCompany), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = "Order shipped successfully." });
    }

    [HttpGet("me/order-stats")]
    [ProducesResponseType(typeof(ProviderOrderStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDirectOrderStats(CancellationToken ct = default)
    {
        var result = await _getProviderOrderStatsHandler.HandleAsync(new GetProviderOrderStatsQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }
}

/// <summary>Request body for updating provider profile.</summary>
public record UpdateProviderProfileRequest(
    string? ProviderName,
    string? ContactPersonName,
    string? Phone,
    string? Email,
    string? Address
);

/// <summary>Request body for responding to a complaint.</summary>
public record RespondComplaintRequest(string Response, bool MarkResolved = false);

/// <summary>Request body for creating a provider withdrawal request.</summary>
public record CreateProviderWithdrawalRequest(decimal Amount);

/// <summary>Request body for shipping a direct order.</summary>
public record ShipDirectOrderRequest(string TrackingCode, string ShippingCompany);
