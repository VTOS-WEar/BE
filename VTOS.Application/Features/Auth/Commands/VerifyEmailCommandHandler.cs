using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Handler for email verification command.
/// Validates OTP and activates user account.
/// </summary>
public class VerifyEmailCommandHandler : IVerifyEmailCommandHandler
{
    private readonly IApplicationDbContext _context;

    public VerifyEmailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<string>> HandleAsync(
        VerifyEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        var emailVerifications = _context.Set<Domain.Entities.EmailVerification>();

        // Find the most recent unverified OTP for this email
        var verification = await emailVerifications
            .Where(v => v.Email == command.Email && !v.IsVerified)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (verification == null)
        {
            return Result<string>.Failure(
                "No verification request found for this email",
                "NO_VERIFICATION_FOUND");
        }

        // Check if OTP has expired
        if (verification.ExpiresAt < DateTime.UtcNow)
        {
            return Result<string>.Failure(
                "OTP code has expired. Please request a new one.",
                "OTP_EXPIRED");
        }

        // Validate OTP code
        if (verification.OTPCode != command.OTPCode)
        {
            return Result<string>.Failure(
                "Invalid OTP code",
                "INVALID_OTP");
        }

        // Mark verification as verified
        verification.IsVerified = true;

        // Activate user account
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user == null)
        {
            return Result<string>.Failure(
                "User not found",
                "USER_NOT_FOUND");
        }

        user.IsActive = true;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Email verified successfully. You can now login.");
    }
}
