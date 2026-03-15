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
    // Phase 3 — Production Orders
    private readonly IGetProviderProductionOrderListQueryHandler _getProductionOrderListHandler;
    private readonly IGetProviderProductionOrderDetailQueryHandler _getProductionOrderDetailHandler;
    private readonly IAcceptProductionOrderCommandHandler _acceptProductionOrderHandler;
    private readonly ICompleteProductionOrderCommandHandler _completeProductionOrderHandler;
    private readonly IProviderRejectProductionOrderCommandHandler _providerRejectProductionOrderHandler;
    // Phase 4 — Delivery
    private readonly IDeliverProductionOrderCommandHandler _deliverHandler;
    private readonly IGetDeliveryStatusQueryHandler _getDeliveryStatusHandler;

    public ProvidersController(
        ICurrentUserService currentUser,
        IGetProviderProfileQueryHandler getProfileHandler,
        IUpdateProviderProfileCommandHandler updateProfileHandler,
        IGetContractsQueryHandler getContractsHandler,
        IGetContractDetailQueryHandler getContractDetailHandler,
        IApproveContractCommandHandler approveContractHandler,
        IRejectContractCommandHandler rejectContractHandler,
        // Phase 3
        IGetProviderProductionOrderListQueryHandler getProductionOrderListHandler,
        IGetProviderProductionOrderDetailQueryHandler getProductionOrderDetailHandler,
        IAcceptProductionOrderCommandHandler acceptProductionOrderHandler,
        ICompleteProductionOrderCommandHandler completeProductionOrderHandler,
        IProviderRejectProductionOrderCommandHandler providerRejectProductionOrderHandler,
        // Phase 4
        IDeliverProductionOrderCommandHandler deliverHandler,
        IGetDeliveryStatusQueryHandler getDeliveryStatusHandler)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _getContractsHandler = getContractsHandler;
        _getContractDetailHandler = getContractDetailHandler;
        _approveContractHandler = approveContractHandler;
        _rejectContractHandler = rejectContractHandler;
        _getProductionOrderListHandler = getProductionOrderListHandler;
        _getProductionOrderDetailHandler = getProductionOrderDetailHandler;
        _acceptProductionOrderHandler = acceptProductionOrderHandler;
        _completeProductionOrderHandler = completeProductionOrderHandler;
        _providerRejectProductionOrderHandler = providerRejectProductionOrderHandler;
        _deliverHandler = deliverHandler;
        _getDeliveryStatusHandler = getDeliveryStatusHandler;
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
