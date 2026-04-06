using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Children.Commands;
using VTOS.Application.Features.Children.DTOs;
using VTOS.Application.Features.Children.Queries;
using VTOS.Application.Features.Users.Validators;
using VTOS.Domain.Enums;

namespace VTOS.API.Controllers;
[ApiController]
[Route("api/children")]
[Authorize(Roles = "Parent")]
public class ChildrenController : Controller
{
    private readonly ICurrentUserService _currentUser;

    private readonly IGetMyChildProfileQueryHandler _getMyChildProfileHandler;
    private readonly IGetChildProfileQueryHandler _getChildProfileHandler;
    private readonly IUpdateChildProfileCommandHandler _updateChildProfileHandler;
    private readonly IValidator<UpdateChildProfileCommand> _updateChildProfileValidator;
    private readonly IUpdateChildAvatarCommandHandler _updateChildAvatarHandler;

    public ChildrenController(
        ICurrentUserService currentUser,
        IGetMyChildProfileQueryHandler getMyChildProfileHandler,
        IGetChildProfileQueryHandler getChildProfileHandler,
        IUpdateChildProfileCommandHandler updateChildProfileHandler,
        IValidator<UpdateChildProfileCommand> updateChildProfileValidator,
        IUpdateChildAvatarCommandHandler updateChildAvatarHandler)
    {
        _currentUser = currentUser;
        _getMyChildProfileHandler = getMyChildProfileHandler;
        _getChildProfileHandler = getChildProfileHandler;
        _updateChildProfileHandler = updateChildProfileHandler;
        _updateChildProfileValidator = updateChildProfileValidator;
        _updateChildAvatarHandler = updateChildAvatarHandler;
    }

    /// <summary>
    /// Get my childs 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyChildProfiles(CancellationToken cancellationToken)
    {
        var result = await _getMyChildProfileHandler.HandleAsync(new GetMyChildProfileQuery(_currentUser.UserId), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }
        return Ok(result);
    }
    /// <summary>
    /// Get my childs 
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetChildProfile([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _getChildProfileHandler.HandleAsync(new GetChildProfileQuery(Guid.Parse(id)), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }
        return Ok(result);
    }
    /// <summary>update child profile by id</summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateChildProfile([FromBody] UpdateChildProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateChildProfileCommand(
            Guid.Parse(request.ChildId),
            request.FullName,
            request.DOB,
            request.Grade,
            request.Gender,
            request.HeightCm,
            request.WeightKg
       );
        var validationResult = await _updateChildProfileValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }
        var result = await _updateChildProfileHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>update child avatar by id</summary>
    [HttpPut("{id}/avatar")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateChildAvatar([FromRoute] string id, IFormFile avatar, CancellationToken cancellationToken)
    {
        if (avatar == null || avatar.Length == 0)
        {
            return BadRequest(new { error = "Avatar file is required" });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(avatar.ContentType))
        {
            return BadRequest(new { error = "Only image files (JPEG, PNG, GIF, WebP) are allowed" });
        }

        // Validate file size (5MB max)
        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (avatar.Length > maxFileSize)
        {
            return BadRequest(new { error = "File size must not exceed 5MB" });
        }

        try
        {
            var childId = Guid.Parse(id);
            var command = new UpdateChildAvatarCommand(childId, avatar);
            var result = await _updateChildAvatarHandler.HandleAsync(command, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error, code = result.ErrorCode });
            }
            
            return Ok(result);
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid child ID format" });
        }
    }
}
