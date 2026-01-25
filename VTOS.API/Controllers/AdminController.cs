using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.Admin.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IGetAllUsersQueryHandler _usersHandler;
    private readonly IGetAllFeedbacksQueryHandler _feedbacksHandler;

    public AdminController(
        IGetAllUsersQueryHandler usersHandler,
        IGetAllFeedbacksQueryHandler feedbacksHandler)
    {
        _usersHandler = usersHandler;
        _feedbacksHandler = feedbacksHandler;
    }

    /// <summary>Get all users</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _usersHandler.HandleAsync(new GetAllUsersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get all feedbacks</summary>
    [HttpGet("feedbacks")]
    public async Task<IActionResult> GetFeedbacks(CancellationToken cancellationToken)
    {
        var result = await _feedbacksHandler.HandleAsync(new GetAllFeedbacksQuery(), cancellationToken);
        return Ok(result);
    }
}
