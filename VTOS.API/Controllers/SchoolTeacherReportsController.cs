using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Teachers.Commands;
using VTOS.Application.Features.Teachers.DTOs;
using VTOS.Application.Features.Teachers.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/schools/me/teacher-reports")]
[Authorize(Roles = "School")]
public class SchoolTeacherReportsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetSchoolTeacherReportsQueryHandler _reportsHandler;
    private readonly IReviewTeacherReportCommandHandler _reviewHandler;

    public SchoolTeacherReportsController(
        ICurrentUserService currentUser,
        IGetSchoolTeacherReportsQueryHandler reportsHandler,
        IReviewTeacherReportCommandHandler reviewHandler)
    {
        _currentUser = currentUser;
        _reportsHandler = reportsHandler;
        _reviewHandler = reviewHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TeacherReportListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports([FromQuery] Guid? classGroupId, [FromQuery] string? status, CancellationToken ct = default)
    {
        var result = await _reportsHandler.HandleAsync(new GetSchoolTeacherReportsQuery(_currentUser.UserId, classGroupId, status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpPut("{reportId:guid}/review")]
    [ProducesResponseType(typeof(TeacherReportListItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Review(Guid reportId, [FromBody] ReviewTeacherReportRequestDto request, CancellationToken ct = default)
    {
        var result = await _reviewHandler.HandleAsync(new ReviewTeacherReportCommand(_currentUser.UserId, reportId, request), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }
}
