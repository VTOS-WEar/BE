using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VTOS.Application.Features.Auth.Commands;
using VTOS.Application.Features.Auth.DTOs;
using VTOS.Application.Features.Auth.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRegisterCommandHandler _registerHandler;
    private readonly ILoginQueryHandler _loginHandler;
    private readonly IVerifyEmailCommandHandler _verifyEmailHandler;
    private readonly ResendOTPCommandHandler _resendOTPHandler;
    private readonly VerifyPhoneCommandHandler _verifyPhoneHandler;
    private readonly ForgotPasswordCommandHandler _forgotPasswordHandler;
    private readonly ResetPasswordCommandHandler _resetPasswordHandler;
    private readonly RequestChangePasswordOTPCommandHandler _requestChangePasswordOTPHandler;
    private readonly ChangePasswordCommandHandler _changePasswordHandler;
    private readonly IValidator<RegisterCommand> _registerValidator;
    private readonly IValidator<LoginQuery> _loginValidator;
    private readonly IValidator<ForgotPasswordCommand> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordCommand> _resetPasswordValidator;
    private readonly IValidator<ChangePasswordCommand> _changePasswordValidator;

    public AuthController(
        IRegisterCommandHandler registerHandler,
        ILoginQueryHandler loginHandler,
        IVerifyEmailCommandHandler verifyEmailHandler,
        ResendOTPCommandHandler resendOTPHandler,
        VerifyPhoneCommandHandler verifyPhoneHandler,
        ForgotPasswordCommandHandler forgotPasswordHandler,
        ResetPasswordCommandHandler resetPasswordHandler,
        RequestChangePasswordOTPCommandHandler requestChangePasswordOTPHandler,
        ChangePasswordCommandHandler changePasswordHandler,
        IValidator<RegisterCommand> registerValidator,
        IValidator<LoginQuery> loginValidator,
        IValidator<ForgotPasswordCommand> forgotPasswordValidator,
        IValidator<ResetPasswordCommand> resetPasswordValidator,
        IValidator<ChangePasswordCommand> changePasswordValidator)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _verifyEmailHandler = verifyEmailHandler;
        _resendOTPHandler = resendOTPHandler;
        _verifyPhoneHandler = verifyPhoneHandler;
        _forgotPasswordHandler = forgotPasswordHandler;
        _resetPasswordHandler = resetPasswordHandler;
        _requestChangePasswordOTPHandler = requestChangePasswordOTPHandler;
        _changePasswordHandler = changePasswordHandler;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    /// <summary>
    /// Register a new user account (NO phone - collected after first login).
    /// Sends OTP to email for verification.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.FullName,
            request.RoleName
        );

        // Validate
        var validationResult = await _registerValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Execute
        var result = await _registerHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Verify email with OTP code.
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.Email, request.OTPCode);
        var result = await _verifyEmailHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(new { message = result.Value });
    }

    /// <summary>
    /// Resend OTP code to email.
    /// </summary>
    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendOTP([FromBody] ResendOTPRequest request, CancellationToken cancellationToken)
    {
        var command = new ResendOTPCommand(request.Email);
        var result = await _resendOTPHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(new { message = result.Value });
    }

    /// <summary>
    /// Login with email and password.
    /// Requires email verification.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var query = new LoginQuery(request.Email, request.Password);

        // Validate
        var validationResult = await _loginValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Execute
        var result = await _loginHandler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "INVALID_CREDENTIALS" || result.ErrorCode == "EMAIL_NOT_VERIFIED")
            {
                return Unauthorized(new { error = result.Error, code = result.ErrorCode });
            }
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Verify phone number and link children from StudentDataImport.
    /// Requires authentication.
    /// </summary>
    [Authorize]
    [HttpPost("verify-phone")]
    [ProducesResponseType(typeof(VerifyPhoneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request, CancellationToken cancellationToken)
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        var command = new VerifyPhoneCommand(userId, request.Phone);
        var result = await _verifyPhoneHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Request password reset. Sends reset link to email if account exists.
    /// Always returns same message regardless of email existence (security).
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);

        // Validate
        var validationResult = await _forgotPasswordValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Execute
        var result = await _forgotPasswordHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Reset password using token from email.
    /// Token is validated and password is updated.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);

        // Validate
        var validationResult = await _resetPasswordValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Execute
        var result = await _resetPasswordHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Request OTP for password change.
    /// Requires authentication.
    /// </summary>
    [Authorize]
    [HttpPost("change-password/request-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestChangePasswordOTP(CancellationToken cancellationToken)
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        var command = new RequestChangePasswordOTPCommand(userId);
        var result = await _requestChangePasswordOTPHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(new { message = result.Value });
    }

    /// <summary>
    /// Change password with OTP verification.
    /// Requires authentication.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        var command = new ChangePasswordCommand(userId, request.OTP, request.CurrentPassword, request.NewPassword);

        // Validate
        var validationResult = await _changePasswordValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Execute
        var result = await _changePasswordHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(new ChangePasswordResponse(result.Value));
    }
}

// Request DTOs
public record VerifyEmailRequest(string Email, string OTPCode);
public record ResendOTPRequest(string Email);
public record VerifyPhoneRequest(string Phone);

