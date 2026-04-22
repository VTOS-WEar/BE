using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/schools/me/classes")]
[Authorize(Roles = "School")]
public class SchoolClassesController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetSchoolClassesOverviewQueryHandler _overviewHandler;
    private readonly IGetSchoolClassDetailQueryHandler _detailHandler;

    public SchoolClassesController(
        ICurrentUserService currentUser,
        IGetSchoolClassesOverviewQueryHandler overviewHandler,
        IGetSchoolClassDetailQueryHandler detailHandler)
    {
        _currentUser = currentUser;
        _overviewHandler = overviewHandler;
        _detailHandler = detailHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SchoolClassesOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview([FromQuery] string? academicYear = null, CancellationToken ct = default)
    {
        var result = await _overviewHandler.HandleAsync(new GetSchoolClassesOverviewQuery(_currentUser.UserId, academicYear), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassGroupDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct = default)
    {
        var result = await _detailHandler.HandleAsync(new GetSchoolClassDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}
