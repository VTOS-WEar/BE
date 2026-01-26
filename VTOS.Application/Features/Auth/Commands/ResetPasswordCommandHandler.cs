using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Handler for reset password command.
/// Verifies token hash in User table, checks expiry, updates password.
/// </summary>
public class ResetPasswordCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<ResetPasswordResponse>> HandleAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        // Hash the incoming token to compare with stored hash
        var tokenHash = TokenGenerator.HashToken(command.Token);

        // Find user with matching token hash that hasn't expired
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.PasswordResetToken == tokenHash && 
                u.PasswordResetTokenExpiry != null &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow &&
                !u.IsDeleted, 
                cancellationToken);

        if (user == null)
        {
            return Result<ResetPasswordResponse>.Failure(
                "Invalid or expired password reset token.",
                "INVALID_TOKEN");
        }

        // Update user password
        user.PasswordHash = _passwordHasher.HashPassword(command.NewPassword);

        // Clear token (single-use)
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ResetPasswordResponse>.Success(
            new ResetPasswordResponse("Password has been reset successfully. You can now log in with your new password."));
    }
}
