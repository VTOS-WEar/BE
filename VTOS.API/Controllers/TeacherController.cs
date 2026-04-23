using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Teachers.Commands;
using VTOS.Application.Features.Teachers.DTOs;
using VTOS.Application.Features.Teachers.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/teacher")]
[Authorize(Roles = "HomeroomTeacher")]
public class TeacherController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetTeacherDashboardQueryHandler _dashboardHandler;
    private readonly IGetTeacherReportsQueryHandler _reportsHandler;
    private readonly ISubmitTeacherReportCommandHandler _submitReportHandler;
    private readonly IGetTeacherReminderCandidatesQueryHandler _reminderCandidatesHandler;
    private readonly ISendTeacherReminderCommandHandler _sendReminderHandler;

    public TeacherController(
        ICurrentUserService currentUser,
        IGetTeacherDashboardQueryHandler dashboardHandler,
        IGetTeacherReportsQueryHandler reportsHandler,
        ISubmitTeacherReportCommandHandler submitReportHandler,
        IGetTeacherReminderCandidatesQueryHandler reminderCandidatesHandler,
        ISendTeacherReminderCommandHandler sendReminderHandler)
    {
        _currentUser = currentUser;
        _dashboardHandler = dashboardHandler;
        _reportsHandler = reportsHandler;
        _submitReportHandler = submitReportHandler;
        _reminderCandidatesHandler = reminderCandidatesHandler;
        _sendReminderHandler = sendReminderHandler;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(TeacherDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
    {
        var result = await _dashboardHandler.HandleAsync(new GetTeacherDashboardQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpGet("reports")]
    [ProducesResponseType(typeof(TeacherReportListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports([FromQuery] Guid? classGroupId, [FromQuery] string? status, [FromQuery] string? reportType, CancellationToken ct = default)
    {
        var result = await _reportsHandler.HandleAsync(new GetTeacherReportsQuery(_currentUser.UserId, classGroupId, status, reportType), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpPost("reports")]
    [ProducesResponseType(typeof(TeacherReportListItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitTeacherReportRequestDto request, CancellationToken ct = default)
    {
        var result = await _submitReportHandler.HandleAsync(new SubmitTeacherReportCommand(_currentUser.UserId, request), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpGet("reminders/candidates")]
    [ProducesResponseType(typeof(TeacherReminderCandidatesResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReminderCandidates([FromQuery] Guid classGroupId, CancellationToken ct = default)
    {
        var result = await _reminderCandidatesHandler.HandleAsync(new GetTeacherReminderCandidatesQuery(_currentUser.UserId, classGroupId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpPost("reminders/send")]
    [ProducesResponseType(typeof(TeacherReminderSendResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendReminder([FromBody] SendTeacherReminderRequestDto request, CancellationToken ct = default)
    {
        var result = await _sendReminderHandler.HandleAsync(new SendTeacherReminderCommand(_currentUser.UserId, request), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }
}
