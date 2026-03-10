using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Auth.Commands;
using VTOS.Application.Features.Auth.DTOs;
using VTOS.Application.Features.Users.Commands;
using VTOS.Application.Features.Users.DTOs;
using VTOS.Application.Features.Users.Queries;
using VTOS.Application.Features.Users.Validators;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Parent")]
public class UserController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    
    private readonly IGetProfileQueryHandler _getProfileHandler;
    private readonly IUpdateProfileCommandHandler _updateProfileHandler;
    private readonly IValidator<UpdateProfileCommand> _updateProfileValidator;
    private readonly IUpdateAvatarCommandHandler _updateAvatarHandler;
    private readonly IValidator<UpdateAvatarCommand> _updateAvatarValidator;
    private readonly ISubmitVerificationCommandHandler _submitVerificationHandler;
    private readonly IValidator<SubmitVerificationCommand> _submitVerificationValidator;

    public UserController(
        ICurrentUserService currentUser,
        IGetProfileQueryHandler getProfileHandler,
        IUpdateProfileCommandHandler updateProfileHandler,
        IValidator<UpdateProfileCommand> updateProfileValidator,
        IUpdateAvatarCommandHandler updateAvatarHandler,
        IValidator<UpdateAvatarCommand> updateAvatarValidator,
        ISubmitVerificationCommandHandler submitVerificationHandler,
        IValidator<SubmitVerificationCommand> submitVerificationValidator)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _updateProfileValidator = updateProfileValidator;
        _updateAvatarHandler = updateAvatarHandler;
        _updateAvatarValidator = updateAvatarValidator;
        _submitVerificationHandler = submitVerificationHandler;
        _submitVerificationValidator = submitVerificationValidator;
    }

     /// <summary>
    /// Get user profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _getProfileHandler.HandleAsync(new GetProfileQuery(_currentUser.UserId), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }
        return Ok(result);
    }

    /// <summary>Get all feedbacks</summary>
    [HttpPut("me/profile")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(
           _currentUser.UserId,
           request.FullName,
           request.DOB,
           request.Gender,
           request.Phone,
           request.Email
       );
        var validationResult = await _updateProfileValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }
        var result = await _updateProfileHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }
    /// <summary>Get all feedbacks</summary>
    [HttpPut("me/avatar")]
    [ProducesResponseType(typeof(UpdateAvatarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAvatar([FromForm] UpdateAvatarRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAvatarCommand(
           _currentUser.UserId,
           request.Avatar
       );

        var validationResult = await _updateAvatarValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _updateAvatarHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result);
    }

    /// <summary>
    /// Update user profile with avatar, name, and phone
    /// </summary>
    [HttpPost("me/verify")]
    [ProducesResponseType(typeof(SubmitVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitVerification([FromForm] SubmitVerificationRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitVerificationCommand(
            _currentUser.UserId,
            request.FullName,
            request.Phone,
            request.Avatar
        );

        var validationResult = await _submitVerificationValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _submitVerificationHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result);
    }
}
