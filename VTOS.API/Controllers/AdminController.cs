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

    public AdminController(
        IGetAllUsersQueryHandler usersHandler,
        IGetAllFeedbacksQueryHandler feedbacksHandler,
        IApproveUserCommandHandler approveHandler,
        ISuspendUserCommandHandler suspendHandler,
        IRemoveFeedbackCommandHandler removeFeedbackHandler)
    {
        _usersHandler = usersHandler;
        _feedbacksHandler = feedbacksHandler;
        _approveHandler = approveHandler;
        _suspendHandler = suspendHandler;
        _removeFeedbackHandler = removeFeedbackHandler;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var result = await _usersHandler.HandleAsync(new GetAllUsersQuery(), ct);
        return Ok(result);
    }

    [HttpGet("feedbacks")]
    public async Task<IActionResult> GetFeedbacks(CancellationToken ct)
    {
        var result = await _feedbacksHandler.HandleAsync(new GetAllFeedbacksQuery(), ct);
        return Ok(result);
    }

    // ✅ Approve User
    [HttpPost("users/{id}/approve")]
    public async Task<IActionResult> ApproveUser(Guid id, CancellationToken ct)
    {
        var success = await _approveHandler.HandleAsync(
            new ApproveUserCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    // ✅ Suspend User
    [HttpPost("users/{id}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, CancellationToken ct)
    {
        var success = await _suspendHandler.HandleAsync(
            new SuspendUserCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    // ✅ Remove Feedback
    [HttpDelete("feedback/{id}")]
    public async Task<IActionResult> RemoveFeedback(Guid id, CancellationToken ct)
    {
        var success = await _removeFeedbackHandler.HandleAsync(
            new RemoveFeedbackCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }
}
