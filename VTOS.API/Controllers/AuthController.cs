using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VTOS.Application.Features.Auth.Commands;
using VTOS.Application.Features.Auth.Commands.TwoFactor;
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
    // 2FA
    private readonly ISetup2FACommandHandler _setup2FAHandler;
    private readonly IConfirm2FACommandHandler _confirm2FAHandler;
    private readonly IDisable2FACommandHandler _disable2FAHandler;
    private readonly IVerify2FACommandHandler _verify2FAHandler;
    // Google OAuth
    private readonly IGoogleLoginCommandHandler _googleLoginHandler;

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
        IValidator<ChangePasswordCommand> changePasswordValidator,
        ISetup2FACommandHandler setup2FAHandler,
        IConfirm2FACommandHandler confirm2FAHandler,
        IDisable2FACommandHandler disable2FAHandler,
        IVerify2FACommandHandler verify2FAHandler,
        IGoogleLoginCommandHandler googleLoginHandler)
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
        _setup2FAHandler = setup2FAHandler;
        _confirm2FAHandler = confirm2FAHandler;
        _disable2FAHandler = disable2FAHandler;
        _verify2FAHandler = verify2FAHandler;
        _googleLoginHandler = googleLoginHandler;
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
            request.RoleName,
            request.AcceptedTerms,
            request.TermsVersion
        );

        // Validate
        var validationResult = await _registerValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            if (validationResult.Errors.Any(e =>
                    e.PropertyName == nameof(RegisterCommand.AcceptedTerms) ||
                    e.PropertyName == nameof(RegisterCommand.TermsVersion)))
            {
                return BadRequest(new
                {
                    error = "You must accept the terms of use",
                    code = "TERMS_NOT_ACCEPTED"
                });
            }

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
    /// If 2FA is enabled, returns a temp token instead of JWT.
    /// If role requires 2FA but not set up, returns requiresTwoFactorSetup flag.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var query = new LoginQuery(request.Email, request.Password, request.TurnstileToken);

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

    // ═══════════════════════════════════════════════════════════════
    //  TWO-FACTOR AUTHENTICATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verify 2FA code during login (step 2).
    /// Accepts TOTP code from authenticator app or a recovery code.
    /// </summary>
    [HttpPost("verify-2fa")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request, CancellationToken ct)
    {
        var command = new Verify2FACommand(request.TwoFactorToken, request.Code);
        var result = await _verify2FAHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Initiate 2FA setup. Returns QR code URI and manual key.
    /// Requires authentication.
    /// </summary>
    [Authorize]
    [HttpPost("2fa/setup")]
    [ProducesResponseType(typeof(Setup2FAResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Setup2FA(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid token" });

        var result = await _setup2FAHandler.HandleAsync(new Setup2FACommand(userId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Confirm 2FA setup with first TOTP code from authenticator.
    /// Returns recovery codes on success.
    /// </summary>
    [Authorize]
    [HttpPost("2fa/confirm")]
    [ProducesResponseType(typeof(Confirm2FAResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm2FA([FromBody] Confirm2FACodeRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid token" });

        var result = await _confirm2FAHandler.HandleAsync(new Confirm2FACommand(userId, request.Code), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Disable 2FA. Requires current TOTP code for security.
    /// </summary>
    [Authorize]
    [HttpPost("2fa/disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable2FA([FromBody] Confirm2FACodeRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid token" });

        var result = await _disable2FAHandler.HandleAsync(new Disable2FACommand(userId, request.Code), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(new { message = result.Value });
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

    /// <summary>
    /// Login or register via Google OAuth.
    /// Validates Google ID token, creates or links account, and returns JWT.
    /// </summary>
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken ct)
    {
        var command = new GoogleLoginCommand(request.IdToken);
        var result = await _googleLoginHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }
}

// Request DTOs
public record GoogleLoginRequest(string IdToken);
public record VerifyEmailRequest(string Email, string OTPCode);
public record ResendOTPRequest(string Email);
public record VerifyPhoneRequest(string Phone);
public record Verify2FARequest(string TwoFactorToken, string Code);
public record Confirm2FACodeRequest(string Code);
