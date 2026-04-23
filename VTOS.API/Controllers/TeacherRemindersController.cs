using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Teachers.Commands;
using VTOS.Application.Features.Teachers.DTOs;
using VTOS.Application.Features.Teachers.Queries;

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
    public async Task<IActionResult> Send([FromBody] SendTeacherReminderRequestDto request, CancellationToken ct = default)
    {
        var result = await _sendReminderHandler.HandleAsync(
            new SendTeacherReminderCommand(_currentUser.UserId, request), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}
