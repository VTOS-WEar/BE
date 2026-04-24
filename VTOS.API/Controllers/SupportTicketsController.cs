using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.SupportTickets;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/support-tickets")]
[Authorize(Roles = "Parent,Provider,School,HomeroomTeacher")]
public class SupportTicketsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICreateSupportTicketCommandHandler _createHandler;
    private readonly IGetMySupportTicketsQueryHandler _listHandler;
    private readonly IGetMySupportTicketDetailQueryHandler _detailHandler;

    public SupportTicketsController(
        ICurrentUserService currentUser,
        ICreateSupportTicketCommandHandler createHandler,
        IGetMySupportTicketsQueryHandler listHandler,
        IGetMySupportTicketDetailQueryHandler detailHandler)
    {
        _currentUser = currentUser;
        _createHandler = createHandler;
        _listHandler = listHandler;
        _detailHandler = detailHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SupportTicketListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _listHandler.HandleAsync(
            new GetMySupportTicketsQuery(_currentUser.UserId, page, pageSize, status),
            ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupportTicketResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMineById(Guid id, CancellationToken ct = default)
    {
        var result = await _detailHandler.HandleAsync(
            new GetMySupportTicketDetailQuery(_currentUser.UserId, id),
            ct);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SupportTicketResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequestDto request, CancellationToken ct = default)
    {
        var result = await _createHandler.HandleAsync(
            new CreateSupportTicketCommand(_currentUser.UserId, request),
            ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}
