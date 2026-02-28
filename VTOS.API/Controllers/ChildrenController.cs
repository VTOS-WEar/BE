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

    public ChildrenController(
        ICurrentUserService currentUser,
        IGetMyChildProfileQueryHandler getMyChildProfileHandler,
        IGetChildProfileQueryHandler getChildProfileHandler,
        IUpdateChildProfileCommandHandler updateChildProfileHandler,
        IValidator<UpdateChildProfileCommand> updateChildProfileValidator)
    {
        _currentUser = currentUser;
        _getMyChildProfileHandler = getMyChildProfileHandler;
        _getChildProfileHandler = getChildProfileHandler;
        _updateChildProfileHandler = updateChildProfileHandler;
        _updateChildProfileValidator = updateChildProfileValidator;
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
}
