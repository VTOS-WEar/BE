using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command for resending OTP code.
/// </summary>
public record ResendOTPCommand(string Email);

/// <summary>
/// Handler for resending OTP code.
/// Rate limited to prevent abuse.
/// </summary>
public class ResendOTPCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public ResendOTPCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result<string>> HandleAsync(
        ResendOTPCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user == null)
        {
            return Result<string>.Failure(
                "Email not found",
                "EMAIL_NOT_FOUND");
        }

        // Check if already verified
        if (user.IsActive)
        {
            return Result<string>.Failure(
                "Email already verified",
                "ALREADY_VERIFIED");
        }

        // Rate limiting: Check recent OTP requests (max 3 in last 10 minutes)
        var recentOTPs = await _context.EmailVerifications
            .Where(v => v.Email == command.Email && 
                       v.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
            .CountAsync(cancellationToken);

        if (recentOTPs >= 3)
        {
            return Result<string>.Failure(
                "Too many OTP requests. Please try again later.",
                "RATE_LIMIT_EXCEEDED");
        }

        // Generate new OTP
        var otpCode = OTPGenerator.Generate();
        var verification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            OTPCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(verification);
        await _context.SaveChangesAsync(cancellationToken);

        // Send OTP email
        try
        {
            await _emailService.SendOTPEmailAsync(command.Email, otpCode, cancellationToken);
        }
        catch (Exception)
        {
            // Log error but return success (OTP is in database)
        }

        return Result<string>.Success("OTP code sent to your email");
    }
}
