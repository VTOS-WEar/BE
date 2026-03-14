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
/// UC-48: Provider Dashboard & Profile
/// Phase 2: Contract review + approve/reject
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

    public ProvidersController(
        ICurrentUserService currentUser,
        IGetProviderProfileQueryHandler getProfileHandler,
        IUpdateProviderProfileCommandHandler updateProfileHandler,
        IGetContractsQueryHandler getContractsHandler,
        IGetContractDetailQueryHandler getContractDetailHandler,
        IApproveContractCommandHandler approveContractHandler,
        IRejectContractCommandHandler rejectContractHandler)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _getContractsHandler = getContractsHandler;
        _getContractDetailHandler = getContractDetailHandler;
        _approveContractHandler = approveContractHandler;
        _rejectContractHandler = rejectContractHandler;
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
}

/// <summary>Request body for updating provider profile.</summary>
public record UpdateProviderProfileRequest(
    string? ProviderName,
    string? ContactPersonName,
    string? Phone,
    string? Email,
    string? Address
);
