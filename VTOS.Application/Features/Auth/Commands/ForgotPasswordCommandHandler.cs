using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Settings;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Handler for forgot password command.
/// Generates secure token, stores hash in User table, sends reset email.
/// Always returns same message regardless of email existence (security).
/// </summary>
public class ForgotPasswordCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly string _frontendBaseUrl;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IOptions<FrontendSettings> frontendSettings)
    {
        _context = context;
        _emailService = emailService;
        _frontendBaseUrl = frontendSettings.Value.BaseUrl;
    }

    public async Task<Result<ForgotPasswordResponse>> HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        // Security: Always return same message regardless of email existence
        const string successMessage = "If an account with that email exists, we have sent a password reset link.";

        // Check if email exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            // Don't reveal that email doesn't exist
            return Result<ForgotPasswordResponse>.Success(new ForgotPasswordResponse(successMessage));
        }

        // Generate secure 64-character token
        var rawToken = TokenGenerator.GenerateSecureToken(64);
        var tokenHash = TokenGenerator.HashToken(rawToken);

        // Store token hash and expiry directly in User table
        user.PasswordResetToken = tokenHash;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // 1 hour expiry

        await _context.SaveChangesAsync(cancellationToken);

        // Build reset link (raw token sent to user, not the hash)
        var resetLink = $"{_frontendBaseUrl}/reset-password?token={rawToken}";

        // Send email (don't fail if email fails)
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);
        }
        catch (Exception)
        {
            // Log error but don't fail the request
            // Token is still valid, user can request again
        }

        return Result<ForgotPasswordResponse>.Success(new ForgotPasswordResponse(successMessage));
    }
}
