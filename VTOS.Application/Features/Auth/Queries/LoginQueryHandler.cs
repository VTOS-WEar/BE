using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Queries;

/// <summary>
/// Handler for user login query.
/// Checks if user email is verified before allowing login.
/// </summary>
public class LoginQueryHandler : ILoginQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginQueryHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResponse>> HandleAsync(
        LoginQuery query,
        CancellationToken cancellationToken = default)
    {
        // Find user by email (include Role for response)
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == query.Email, cancellationToken);

        if (user == null)
        {
            return Result<LoginResponse>.Failure(
                "Invalid email or password",
                "INVALID_CREDENTIALS");
        }

        // Check if email is verified
        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure(
                "Email not verified. Please verify your email first.",
                "EMAIL_NOT_VERIFIED");
        }

        // Check if user is deleted
        if (user.IsDeleted)
        {
            return Result<LoginResponse>.Failure(
                "Account is disabled",
                "ACCOUNT_DISABLED");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, query.Password))
        {
            return Result<LoginResponse>.Failure(
                "Invalid email or password",
                "INVALID_CREDENTIALS");
        }

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(user);
        var expiresIn = _jwtTokenGenerator.GetExpiryMinutes() * 60; // Convert to seconds

        return Result<LoginResponse>.Success(new LoginResponse(
            token,
            expiresIn,
            new UserDto(
                user.Id,
                user.Email,
                user.FullName,
                user.Role.RoleName,
                user.Phone
            )
        ));
    }
}
