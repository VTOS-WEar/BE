using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.AccountRequests.Commands;
using VTOS.Application.Features.AccountRequests.DTOs;
using VTOS.Application.Features.AccountRequests.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api")]
public class AccountRequestsController : ControllerBase
{
    private readonly ISubmitAccountRequestCommandHandler _submitHandler;
    private readonly IGetAccountRequestsQueryHandler _listHandler;
    private readonly IGetAccountRequestDetailQueryHandler _detailHandler;
    private readonly ICreateAccountForRequestCommandHandler _createAccountHandler;
    private readonly IRejectAccountRequestCommandHandler _rejectHandler;

    public AccountRequestsController(
        ISubmitAccountRequestCommandHandler submitHandler,
        IGetAccountRequestsQueryHandler listHandler,
        IGetAccountRequestDetailQueryHandler detailHandler,
        ICreateAccountForRequestCommandHandler createAccountHandler,
        IRejectAccountRequestCommandHandler rejectHandler)
    {
        _submitHandler = submitHandler;
        _listHandler = listHandler;
        _detailHandler = detailHandler;
        _createAccountHandler = createAccountHandler;
        _rejectHandler = rejectHandler;
    }

    /// <summary>Public: School/Provider submits partnership request (no auth)</summary>
    [HttpPost("public/account-requests")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitRequest(
        [FromBody] SubmitAccountRequestDto request,
        CancellationToken ct)
    {
        var result = await _submitHandler.HandleAsync(
            new SubmitAccountRequestCommand(request), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return CreatedAtAction(nameof(GetDetail), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Admin: List all account requests (paginated, filterable)</summary>
    [HttpGet("admin/account-requests")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] int? type = null,
        CancellationToken ct = default)
    {
        var result = await _listHandler.HandleAsync(
            new GetAccountRequestsQuery(page, pageSize, status, type), ct);

        return Ok(result.Value);
    }

    /// <summary>Admin: Get account request detail</summary>
    [HttpGet("admin/account-requests/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await _detailHandler.HandleAsync(
            new GetAccountRequestDetailQuery(id), ct);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>Admin: Create account for approved request</summary>
    [HttpPost("admin/account-requests/{id:guid}/create-account")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAccount(
        Guid id,
        [FromBody] CreateAccountForRequestDto request,
        CancellationToken ct)
    {
        var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _createAccountHandler.HandleAsync(
            new CreateAccountForRequestCommand(adminUserId, id, request), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(new { error = result.Error, code = result.ErrorCode }),
                _ => BadRequest(new { error = result.Error, code = result.ErrorCode })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>Admin: Reject an account request</summary>
    [HttpPost("admin/account-requests/{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectAccountRequestDto request,
        CancellationToken ct)
    {
        var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _rejectHandler.HandleAsync(
            new RejectAccountRequestCommand(adminUserId, id, request.Reason), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(new { error = result.Error, code = result.ErrorCode }),
                _ => BadRequest(new { error = result.Error, code = result.ErrorCode })
            };
        }

        return Ok(result.Value);
    }
}
