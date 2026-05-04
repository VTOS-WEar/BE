using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Handler for requesting OTP for password change.
/// Generates OTP, stores it, and sends email to user.
/// </summary>
public class RequestChangePasswordOTPCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestChangePasswordOTPCommandHandler> _logger;

    public RequestChangePasswordOTPCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<RequestChangePasswordOTPCommandHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<string>> HandleAsync(
        RequestChangePasswordOTPCommand command,
        CancellationToken cancellationToken = default)
    {
        var emailVerifications = _context.Set<EmailVerification>();

        // Get user email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return Result<string>.Failure("User not found.", "USER_NOT_FOUND");
        }

        // Invalidate any existing unused OTPs for this user (Purpose: ChangePassword)
        var existingOtps = await emailVerifications
            .Where(e => e.Email == user.Email && e.Purpose == "ChangePassword" && !e.IsVerified)
            .ToListAsync(cancellationToken);

        foreach (var otp in existingOtps)
        {
            otp.IsVerified = true; // Mark as used/invalidated
        }

        // Generate new 6-digit OTP
        var otpCode = OTPGenerator.Generate();

        // Store OTP
        var emailVerification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            Email = user.Email,
            OTPCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow,
            Purpose = "ChangePassword"
        };

        emailVerifications.Add(emailVerification);
        await _context.SaveChangesAsync(cancellationToken);

        // Send OTP email
        try
        {
            await _emailService.SendChangePasswordOTPEmailAsync(user.Email, otpCode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send change-password OTP to {Email}.", user.Email);
            return Result<string>.Failure("Could not send OTP email.", "EMAIL_SEND_FAILED");
        }

        return Result<string>.Success("OTP has been sent to your email.");
    }
}
