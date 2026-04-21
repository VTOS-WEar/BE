using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/teacher/reports")]
[Authorize(Roles = "HomeroomTeacher")]
public class TeacherReportsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetTeacherReportsQueryHandler _getReportsHandler;
    private readonly ISubmitTeacherReportCommandHandler _submitReportHandler;

    public TeacherReportsController(
        ICurrentUserService currentUser,
        IGetTeacherReportsQueryHandler getReportsHandler,
        ISubmitTeacherReportCommandHandler submitReportHandler)
    {
        _currentUser = currentUser;
        _getReportsHandler = getReportsHandler;
        _submitReportHandler = submitReportHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TeacherReportListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(
        [FromQuery] Guid? classGroupId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? reportType = null,
        CancellationToken ct = default)
    {
        var result = await _getReportsHandler.HandleAsync(
            new GetTeacherReportsQuery(_currentUser.UserId, classGroupId, status, reportType), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TeacherReportListItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitTeacherReportRequest request, CancellationToken ct = default)
    {
        var result = await _submitReportHandler.HandleAsync(
            new SubmitTeacherReportCommand(_currentUser.UserId, request.ClassGroupId, request.ReportType, request.Title, request.Content), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }
}

public record SubmitTeacherReportRequest(Guid ClassGroupId, string ReportType, string Title, string Content);
