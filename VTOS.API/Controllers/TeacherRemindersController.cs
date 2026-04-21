using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/teacher/reminders")]
[Authorize(Roles = "HomeroomTeacher")]
public class TeacherRemindersController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetTeacherReminderCandidatesQueryHandler _getCandidatesHandler;
    private readonly ISendTeacherReminderCommandHandler _sendReminderHandler;

    public TeacherRemindersController(
        ICurrentUserService currentUser,
        IGetTeacherReminderCandidatesQueryHandler getCandidatesHandler,
        ISendTeacherReminderCommandHandler sendReminderHandler)
    {
        _currentUser = currentUser;
        _getCandidatesHandler = getCandidatesHandler;
        _sendReminderHandler = sendReminderHandler;
    }

    [HttpGet("candidates")]
    [ProducesResponseType(typeof(TeacherReminderCandidatesResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCandidates([FromQuery] Guid classGroupId, CancellationToken ct = default)
    {
        var result = await _getCandidatesHandler.HandleAsync(new GetTeacherReminderCandidatesQuery(_currentUser.UserId, classGroupId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(TeacherReminderSendResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Send([FromBody] SendTeacherReminderRequest request, CancellationToken ct = default)
    {
        var result = await _sendReminderHandler.HandleAsync(
            new SendTeacherReminderCommand(_currentUser.UserId, request.ClassGroupId, request.ParentUserIds, request.Note), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}

public record SendTeacherReminderRequest(Guid ClassGroupId, IReadOnlyList<Guid>? ParentUserIds, string? Note);
