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
    private readonly IApproveContractCommandHandler _approveContractHandler;
    private readonly IRejectContractCommandHandler _rejectContractHandler;
    private readonly IRequestSignOTPCommandHandler _requestSignOTPHandler;
    private readonly ISignContractByProviderCommandHandler _signContractByProviderHandler;
    // Phase 3 — Production Orders
    private readonly IGetProviderProductionOrderListQueryHandler _getProductionOrderListHandler;
    private readonly IGetProviderProductionOrderDetailQueryHandler _getProductionOrderDetailHandler;
    private readonly IAcceptProductionOrderCommandHandler _acceptProductionOrderHandler;
    private readonly ICompleteProductionOrderCommandHandler _completeProductionOrderHandler;
    private readonly IProviderRejectProductionOrderCommandHandler _providerRejectProductionOrderHandler;
    // Phase 4 — Delivery
    private readonly IDeliverProductionOrderCommandHandler _deliverHandler;
    private readonly IGetDeliveryStatusQueryHandler _getDeliveryStatusHandler;
    // Phase 5 — Complaints
    private readonly IGetProviderComplaintsQueryHandler _getProviderComplaintsHandler;
    private readonly IGetProviderComplaintDetailQueryHandler _getProviderComplaintDetailHandler;
    private readonly IRespondComplaintCommandHandler _respondComplaintHandler;
    // Phase 5 — Distribution overview
    private readonly Application.Features.Distribution.IGetProviderDistributionOverviewHandler _distributionOverviewHandler;
    // Withdrawal
    private readonly ICreateProviderWithdrawalRequestCommandHandler _createWithdrawalHandler;
    private readonly IGetProviderIncomingOrdersQueryHandler _getProviderIncomingOrdersHandler;
    private readonly IGetProviderDirectOrderDetailQueryHandler _getProviderDirectOrderDetailHandler;
    private readonly IAcceptDirectOrderCommandHandler _acceptDirectOrderHandler;
    private readonly IUpdateDirectOrderInProductionCommandHandler _updateDirectOrderInProductionHandler;
    private readonly IMarkDirectOrderReadyToShipCommandHandler _markDirectOrderReadyToShipHandler;
    private readonly IShipDirectOrderCommandHandler _shipDirectOrderHandler;
    private readonly IGetProviderOrderStatsQueryHandler _getProviderOrderStatsHandler;

    public ProvidersController(
        ICurrentUserService currentUser,
        IGetProviderProfileQueryHandler getProfileHandler,
        IUpdateProviderProfileCommandHandler updateProfileHandler,
        IGetContractsQueryHandler getContractsHandler,
        IGetContractDetailQueryHandler getContractDetailHandler,
        IApproveContractCommandHandler approveContractHandler,
        IRejectContractCommandHandler rejectContractHandler,
        IRequestSignOTPCommandHandler requestSignOTPHandler,
        ISignContractByProviderCommandHandler signContractByProviderHandler,
        // Phase 3
        IGetProviderProductionOrderListQueryHandler getProductionOrderListHandler,
        IGetProviderProductionOrderDetailQueryHandler getProductionOrderDetailHandler,
        IAcceptProductionOrderCommandHandler acceptProductionOrderHandler,
        ICompleteProductionOrderCommandHandler completeProductionOrderHandler,
        IProviderRejectProductionOrderCommandHandler providerRejectProductionOrderHandler,
        // Phase 4
        IDeliverProductionOrderCommandHandler deliverHandler,
        IGetDeliveryStatusQueryHandler getDeliveryStatusHandler,
        // Phase 5
        IGetProviderComplaintsQueryHandler getProviderComplaintsHandler,
        IGetProviderComplaintDetailQueryHandler getProviderComplaintDetailHandler,
        IRespondComplaintCommandHandler respondComplaintHandler,
        Application.Features.Distribution.IGetProviderDistributionOverviewHandler distributionOverviewHandler,
        ICreateProviderWithdrawalRequestCommandHandler createWithdrawalHandler,
        IGetProviderIncomingOrdersQueryHandler getProviderIncomingOrdersHandler,
        IGetProviderDirectOrderDetailQueryHandler getProviderDirectOrderDetailHandler,
        IAcceptDirectOrderCommandHandler acceptDirectOrderHandler,
        IUpdateDirectOrderInProductionCommandHandler updateDirectOrderInProductionHandler,
        IMarkDirectOrderReadyToShipCommandHandler markDirectOrderReadyToShipHandler,
        IShipDirectOrderCommandHandler shipDirectOrderHandler,
        IGetProviderOrderStatsQueryHandler getProviderOrderStatsHandler)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _getContractsHandler = getContractsHandler;
        _getContractDetailHandler = getContractDetailHandler;
        _approveContractHandler = approveContractHandler;
        _rejectContractHandler = rejectContractHandler;
        _requestSignOTPHandler = requestSignOTPHandler;
        _signContractByProviderHandler = signContractByProviderHandler;
        _getProductionOrderListHandler = getProductionOrderListHandler;
        _getProductionOrderDetailHandler = getProductionOrderDetailHandler;
        _acceptProductionOrderHandler = acceptProductionOrderHandler;
        _completeProductionOrderHandler = completeProductionOrderHandler;
        _providerRejectProductionOrderHandler = providerRejectProductionOrderHandler;
        _deliverHandler = deliverHandler;
        _getDeliveryStatusHandler = getDeliveryStatusHandler;
        _getProviderComplaintsHandler = getProviderComplaintsHandler;
        _getProviderComplaintDetailHandler = getProviderComplaintDetailHandler;
        _respondComplaintHandler = respondComplaintHandler;
        _distributionOverviewHandler = distributionOverviewHandler;
        _createWithdrawalHandler = createWithdrawalHandler;
        _getProviderIncomingOrdersHandler = getProviderIncomingOrdersHandler;
        _getProviderDirectOrderDetailHandler = getProviderDirectOrderDetailHandler;
        _acceptDirectOrderHandler = acceptDirectOrderHandler;
        _updateDirectOrderInProductionHandler = updateDirectOrderInProductionHandler;
        _markDirectOrderReadyToShipHandler = markDirectOrderReadyToShipHandler;
        _shipDirectOrderHandler = shipDirectOrderHandler;
        _getProviderOrderStatsHandler = getProviderOrderStatsHandler;
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

    // ──────── Contract Management (Phase 2) ────────

    /// <summary>List contracts sent to this provider, with optional status filter.</summary>
    [HttpGet("me/contracts")]
    [ProducesResponseType(typeof(List<ContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContracts([FromQuery] string? status, CancellationToken ct)
    {
        var result = await _getContractsHandler.HandleAsync(
            new GetContractsQuery(_currentUser.UserId, "Provider", status), ct);
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

    // ──────── Production Order Management (Phase 3) ────────

    /// <summary>List production orders assigned to this provider, with optional status filter.</summary>
    [HttpGet("me/production-orders")]
    [ProducesResponseType(typeof(GetProviderProductionOrderListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductionOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getProductionOrderListHandler.HandleAsync(
            new GetProviderProductionOrderListQuery(_currentUser.UserId, page, pageSize, status), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get production order detail by ID (provider-scoped).</summary>
    [HttpGet("me/production-orders/{id:guid}")]
    [ProducesResponseType(typeof(ProviderProductionOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductionOrderDetail(Guid id, CancellationToken ct)
    {
        var result = await _getProductionOrderDetailHandler.HandleAsync(
            new GetProviderProductionOrderDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Accept a production order — start production (Approved → InProduction).</summary>
    [HttpPut("me/production-orders/{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptProductionOrder(Guid id, CancellationToken ct)
    {
        var result = await _acceptProductionOrderHandler.HandleAsync(
            new AcceptProductionOrderCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>Complete a production order (InProduction → Completed).</summary>
    [HttpPut("me/production-orders/{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteProductionOrder(Guid id, CancellationToken ct)
    {
        var result = await _completeProductionOrderHandler.HandleAsync(
            new CompleteProductionOrderCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>Reject a production order with a reason.</summary>
    [HttpPut("me/production-orders/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectProductionOrder(Guid id, [FromBody] ProviderRejectProductionOrderRequest request, CancellationToken ct)
    {
        var result = await _providerRejectProductionOrderHandler.HandleAsync(
            new ProviderRejectProductionOrderCommand(_currentUser.UserId, id, request.Reason), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    // ──────── Delivery Management (Phase 4) ────────

    /// <summary>Deliver a partial shipment (Completed → auto Delivered at 100%).</summary>
    [HttpPut("me/production-orders/{id:guid}/deliver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeliverProductionOrder(Guid id, [FromBody] DeliverProductionOrderRequest request, CancellationToken ct)
    {
        var result = await _deliverHandler.HandleAsync(
            new DeliverProductionOrderCommand(_currentUser.UserId, id, request.Quantity, request.Note), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>View delivery status for a production order.</summary>
    [HttpGet("me/production-orders/{id:guid}/delivery-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDeliveryStatus(Guid id, CancellationToken ct)
    {
        var result = await _getDeliveryStatusHandler.HandleAsync(
            new GetDeliveryStatusQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ──────── Complaint Management (Phase 5) ────────

    /// <summary>List complaints sent to this provider.</summary>
    [HttpGet("me/complaints")]
    [ProducesResponseType(typeof(GetProviderComplaintsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderComplaints(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getProviderComplaintsHandler.HandleAsync(
            new GetProviderComplaintsQuery(_currentUser.UserId, page, pageSize, status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get complaint detail by ID (provider-scoped).</summary>
    [HttpGet("me/complaints/{id:guid}")]
    [ProducesResponseType(typeof(Application.Features.Schools.Queries.ComplaintDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderComplaintDetail(Guid id, CancellationToken ct)
    {
        var result = await _getProviderComplaintDetailHandler.HandleAsync(
            new GetProviderComplaintDetailQuery(_currentUser.UserId, id), ct);
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
            new RespondComplaintCommand(_currentUser.UserId, id, request.Response, request.MarkResolved), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    // ──────── Distribution Overview (Phase 5) ────────

    /// <summary>View distribution overview for a production order (read-only).</summary>
    [HttpGet("me/production-orders/{batchId:guid}/distribution-overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDistributionOverview(Guid batchId, CancellationToken ct)
    {
        var result = await _distributionOverviewHandler.HandleAsync(_currentUser.UserId, batchId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
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

    [HttpGet("me/orders")]
    [ProducesResponseType(typeof(ProviderIncomingOrdersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncomingOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getProviderIncomingOrdersHandler.HandleAsync(
            new GetProviderIncomingOrdersQuery(_currentUser.UserId, page, pageSize, status), ct);
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

/// <summary>Request body for delivering uniforms.</summary>
public record DeliverProductionOrderRequest(int Quantity, string? Note);

/// <summary>Request body for updating provider profile.</summary>
public record UpdateProviderProfileRequest(
    string? ProviderName,
    string? ContactPersonName,
    string? Phone,
    string? Email,
    string? Address
);

/// <summary>Request body for provider rejecting a production order.</summary>
public record ProviderRejectProductionOrderRequest(string Reason);

/// <summary>Request body for responding to a complaint.</summary>
public record RespondComplaintRequest(string Response, bool MarkResolved = false);

/// <summary>Request body for creating a provider withdrawal request.</summary>
public record CreateProviderWithdrawalRequest(decimal Amount);

/// <summary>Request body for shipping a direct order.</summary>
public record ShipDirectOrderRequest(string TrackingCode, string ShippingCompany);
