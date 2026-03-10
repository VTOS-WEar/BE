using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.Admin.Commands;
using VTOS.Application.Features.Admin.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IGetAllUsersQueryHandler _usersHandler;
    private readonly IGetAllFeedbacksQueryHandler _feedbacksHandler;
    private readonly IApproveUserCommandHandler _approveHandler;
    private readonly ISuspendUserCommandHandler _suspendHandler;
    private readonly IRemoveFeedbackCommandHandler _removeFeedbackHandler;
    private readonly IGetUserDetailQueryHandler _getUserDetailHandler;
    private readonly IBanUserCommandHandler _banHandler;
    private readonly IUnbanUserCommandHandler _unbanHandler;

    public AdminController(
        IGetAllUsersQueryHandler usersHandler,
        IGetAllFeedbacksQueryHandler feedbacksHandler,
        IApproveUserCommandHandler approveHandler,
        ISuspendUserCommandHandler suspendHandler,
        IRemoveFeedbackCommandHandler removeFeedbackHandler,
        IGetUserDetailQueryHandler getUserDetailHandler,
        IBanUserCommandHandler banHandler,
        IUnbanUserCommandHandler unbanHandler)
    {
        _usersHandler = usersHandler;
        _feedbacksHandler = feedbacksHandler;
        _approveHandler = approveHandler;
        _suspendHandler = suspendHandler;
        _removeFeedbackHandler = removeFeedbackHandler;
        _getUserDetailHandler = getUserDetailHandler;
        _banHandler = banHandler;
        _unbanHandler = unbanHandler;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var result = await _usersHandler.HandleAsync(new GetAllUsersQuery(), ct);
        return Ok(result);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getUserDetailHandler.HandleAsync(
            new GetUserDetailQuery(id), cancellationToken);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("users/{id}/ban")]
    public async Task<IActionResult> BanUser(Guid id, CancellationToken ct)
    {
        var success = await _banHandler.HandleAsync(
            new BanUserCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    [HttpPost("users/{id}/unban")]
    public async Task<IActionResult> UnbanUser(Guid id, CancellationToken ct)
    {
        var success = await _unbanHandler.HandleAsync(
            new UnbanUserCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    [HttpGet("feedbacks")]
    public async Task<IActionResult> GetFeedbacks(CancellationToken ct)
    {
        var result = await _feedbacksHandler.HandleAsync(new GetAllFeedbacksQuery(), ct);
        return Ok(result);
    }

    [HttpPost("users/{id}/approve")]
    public async Task<IActionResult> ApproveUser(Guid id, CancellationToken ct)
    {
        var success = await _approveHandler.HandleAsync(
            new ApproveUserCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    [HttpPost("users/{id}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, CancellationToken ct)
    {
        var success = await _suspendHandler.HandleAsync(
            new SuspendUserCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    [HttpDelete("feedback/{id}")]
    public async Task<IActionResult> RemoveFeedback(Guid id, CancellationToken ct)
    {
        var success = await _removeFeedbackHandler.HandleAsync(
            new RemoveFeedbackCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }
}