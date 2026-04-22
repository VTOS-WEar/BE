using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/school-manager/teacher-reports")]
[Authorize(Roles = "School")]
public class SchoolTeacherReportsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetSchoolTeacherReportsQueryHandler _getReportsHandler;
    private readonly IReviewTeacherReportCommandHandler _reviewHandler;

    public SchoolTeacherReportsController(
        ICurrentUserService currentUser,
        IGetSchoolTeacherReportsQueryHandler getReportsHandler,
        IReviewTeacherReportCommandHandler reviewHandler)
    {
        _currentUser = currentUser;
        _getReportsHandler = getReportsHandler;
        _reviewHandler = reviewHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TeacherReportListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports([FromQuery] Guid? classGroupId = null, [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await _getReportsHandler.HandleAsync(new GetSchoolTeacherReportsQuery(_currentUser.UserId, classGroupId, status), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPut("{reportId:guid}/review")]
    [ProducesResponseType(typeof(TeacherReportListItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Review(Guid reportId, [FromBody] ReviewTeacherReportRequest request, CancellationToken ct = default)
    {
        var result = await _reviewHandler.HandleAsync(new ReviewTeacherReportCommand(_currentUser.UserId, reportId, request.ReviewNote), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}

public record ReviewTeacherReportRequest(string? ReviewNote);
