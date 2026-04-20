using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/teacher/classes")]
[Authorize(Roles = "HomeroomTeacher")]
public class TeacherClassesController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetTeacherClassesOverviewQueryHandler _overviewHandler;
    private readonly IGetTeacherClassDetailQueryHandler _detailHandler;

    public TeacherClassesController(
        ICurrentUserService currentUser,
        IGetTeacherClassesOverviewQueryHandler overviewHandler,
        IGetTeacherClassDetailQueryHandler detailHandler)
    {
        _currentUser = currentUser;
        _overviewHandler = overviewHandler;
        _detailHandler = detailHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TeacherClassesOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken ct = default)
    {
        var result = await _overviewHandler.HandleAsync(new GetTeacherClassesOverviewQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassGroupDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct = default)
    {
        var result = await _detailHandler.HandleAsync(new GetTeacherClassDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}
