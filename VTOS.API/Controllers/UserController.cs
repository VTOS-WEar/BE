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
    private readonly GetMyChildrenQueryHandler _getMyChildrenHandler;
    private readonly FindChildrenCommandHandler _findChildrenHandler;
    private readonly IAddParentBankAccountCommandHandler _addBankAccountHandler;
    private readonly IGetParentAddressesQueryHandler _getParentAddressesHandler;
    private readonly IUpsertParentAddressCommandHandler _upsertParentAddressHandler;
    private readonly IDeleteParentAddressCommandHandler _deleteParentAddressHandler;
    private readonly ISetDefaultParentAddressCommandHandler _setDefaultParentAddressHandler;

    public UserController(
        ICurrentUserService currentUser,
        IGetProfileQueryHandler getProfileHandler,
        IUpdateProfileCommandHandler updateProfileHandler,
        IValidator<UpdateProfileCommand> updateProfileValidator,
        IUpdateAvatarCommandHandler updateAvatarHandler,
        IValidator<UpdateAvatarCommand> updateAvatarValidator,
        ISubmitVerificationCommandHandler submitVerificationHandler,
        IValidator<SubmitVerificationCommand> submitVerificationValidator,
        GetMyChildrenQueryHandler getMyChildrenHandler,
        FindChildrenCommandHandler findChildrenHandler,
        IAddParentBankAccountCommandHandler addBankAccountHandler,
        IGetParentAddressesQueryHandler getParentAddressesHandler,
        IUpsertParentAddressCommandHandler upsertParentAddressHandler,
        IDeleteParentAddressCommandHandler deleteParentAddressHandler,
        ISetDefaultParentAddressCommandHandler setDefaultParentAddressHandler)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _updateProfileValidator = updateProfileValidator;
        _updateAvatarHandler = updateAvatarHandler;
        _updateAvatarValidator = updateAvatarValidator;
        _submitVerificationHandler = submitVerificationHandler;
        _submitVerificationValidator = submitVerificationValidator;
        _getMyChildrenHandler = getMyChildrenHandler;
        _findChildrenHandler = findChildrenHandler;
        _addBankAccountHandler = addBankAccountHandler;
        _getParentAddressesHandler = getParentAddressesHandler;
        _upsertParentAddressHandler = upsertParentAddressHandler;
        _deleteParentAddressHandler = deleteParentAddressHandler;
        _setDefaultParentAddressHandler = setDefaultParentAddressHandler;
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

    /// <summary>
    /// Get all children linked to the current parent.
    /// </summary>
    [HttpGet("me/children")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyChildren(CancellationToken cancellationToken)
    {
        var result = await _getMyChildrenHandler.HandleAsync(
            new GetMyChildrenQuery(_currentUser.UserId), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Find and link children to the current parent based on their stored phone number.
    /// Triggered by the "Tìm trẻ" button in parent profile.
    /// </summary>
    [HttpPost("me/find-children")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FindMyChildren(CancellationToken cancellationToken)
    {
        var result = await _findChildrenHandler.HandleAsync(
            new FindChildrenCommand(_currentUser.UserId), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("me/addresses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyAddresses(CancellationToken cancellationToken)
    {
        var result = await _getParentAddressesHandler.HandleAsync(
            new GetParentAddressesQuery(_currentUser.UserId),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost("me/addresses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAddress([FromBody] UpsertParentAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await _upsertParentAddressHandler.HandleAsync(
            new UpsertParentAddressCommand(
                _currentUser.UserId,
                null,
                request.Label,
                request.RecipientName,
                request.RecipientPhone,
                request.AddressLine,
                request.IsDefault),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPut("me/addresses/{addressId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] UpsertParentAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await _upsertParentAddressHandler.HandleAsync(
            new UpsertParentAddressCommand(
                _currentUser.UserId,
                addressId,
                request.Label,
                request.RecipientName,
                request.RecipientPhone,
                request.AddressLine,
                request.IsDefault),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpDelete("me/addresses/{addressId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken cancellationToken)
    {
        var result = await _deleteParentAddressHandler.HandleAsync(
            new DeleteParentAddressCommand(_currentUser.UserId, addressId),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(new { message = "Address deleted." });
    }

    [HttpPut("me/addresses/{addressId:guid}/default")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetDefaultAddress(Guid addressId, CancellationToken cancellationToken)
    {
        var result = await _setDefaultParentAddressHandler.HandleAsync(
            new SetDefaultParentAddressCommand(_currentUser.UserId, addressId),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(new { message = "Default address updated." });
    }


    /// <summary>
    /// Add a bank account for the current parent user.
    /// </summary>
    [HttpPost("me/bank-accounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddBankAccount(
        [FromBody] AddParentBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addBankAccountHandler.HandleAsync(
            new AddParentBankAccountCommand(
                _currentUser.UserId,
                request.BankName,
                request.BankCode,
                request.AccountNumber,
                request.AccountHolderName,
                request.IsDefault), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}


