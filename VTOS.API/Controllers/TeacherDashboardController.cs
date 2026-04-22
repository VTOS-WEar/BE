using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/teacher/dashboard")]
[Authorize(Roles = "HomeroomTeacher")]
public class TeacherDashboardController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetTeacherDashboardQueryHandler _handler;

    public TeacherDashboardController(ICurrentUserService currentUser, IGetTeacherDashboardQueryHandler handler)
    {
        _currentUser = currentUser;
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TeacherDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
    {
        var result = await _handler.HandleAsync(new GetTeacherDashboardQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}
