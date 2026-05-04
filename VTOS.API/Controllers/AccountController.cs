using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Account.Commands;
using VTOS.Application.Features.Account.DTOs;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/account")]
[Authorize(Roles = "Parent,Provider,School,HomeroomTeacher")]
public class AccountController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUpdateAccountEmailCommandHandler _updateEmailHandler;
    private readonly IValidator<UpdateAccountEmailCommand> _updateEmailValidator;

    public AccountController(
        ICurrentUserService currentUser,
        IUpdateAccountEmailCommandHandler updateEmailHandler,
        IValidator<UpdateAccountEmailCommand> updateEmailValidator)
    {
        _currentUser = currentUser;
        _updateEmailHandler = updateEmailHandler;
        _updateEmailValidator = updateEmailValidator;
    }

    [HttpPut("email")]
    [ProducesResponseType(typeof(UpdateAccountEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateEmail(
        [FromBody] UpdateAccountEmailRequest request,
        CancellationToken ct)
    {
        var command = new UpdateAccountEmailCommand(_currentUser.UserId, request.Email);
        var validationResult = await _updateEmailValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var result = await _updateEmailHandler.HandleAsync(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}
