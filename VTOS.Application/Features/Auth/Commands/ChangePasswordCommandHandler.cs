using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Handler for changing password with OTP verification.
/// Verifies OTP, validates current password, updates to new password.
/// </summary>
public class ChangePasswordCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<string>> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return Result<string>.Failure("User not found.", "USER_NOT_FOUND");
        }

        // Verify OTP
        var verification = await _context.EmailVerifications
            .FirstOrDefaultAsync(e => 
                e.Email == user.Email && 
                e.OTPCode == command.OTP && 
                e.Purpose == "ChangePassword" &&
                !e.IsVerified && 
                e.ExpiresAt > DateTime.UtcNow, 
                cancellationToken);

        if (verification == null)
        {
            return Result<string>.Failure("Invalid or expired OTP.", "INVALID_OTP");
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, command.CurrentPassword))
        {
            return Result<string>.Failure("Current password is incorrect.", "WRONG_PASSWORD");
        }

        // Update password
        user.PasswordHash = _passwordHasher.HashPassword(command.NewPassword);

        // Mark OTP as used
        verification.IsVerified = true;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Password has been changed successfully.");
    }
}
