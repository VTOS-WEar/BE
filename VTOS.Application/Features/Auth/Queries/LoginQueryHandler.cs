using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Queries;

/// <summary>
/// Handler for user login query.
/// Checks email verification, 2FA status, and role-based 2FA requirements.
/// </summary>
public class LoginQueryHandler : ILoginQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    // Roles that MUST have 2FA enabled
    private static readonly HashSet<string> MandatoryTwoFactorRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "School", "Provider"
    };

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

        // ── 2FA Check ────────────────────────────────────────────────
        var roleName = user.Role.RoleName;
        var isMandatoryRole = MandatoryTwoFactorRoles.Contains(roleName);

        // Case 1: 2FA is enabled → require TOTP verification
        if (user.IsTwoFactorEnabled)
        {
            var tempToken = _jwtTokenGenerator.GenerateTwoFactorToken(user.Id);
            var userDto = new UserDto(user.Id, user.Email, user.FullName, roleName, user.Phone);
            return Result<LoginResponse>.Success(new LoginResponse(
                "", 0, userDto,
                RequiresTwoFactor: true,
                TwoFactorToken: tempToken
            ));
        }

        // Case 2: Role requires 2FA but not yet set up → force setup
        if (isMandatoryRole && !user.IsTwoFactorEnabled)
        {
            // Update last login so they can access the setup endpoint
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Still generate a real token (they need to be authenticated to set up 2FA)
            Guid? providerId = null;
            Guid? schoolId = null;
            var provMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, cancellationToken);
            if (provMgr != null) providerId = provMgr.ProviderID;
            var schMgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, cancellationToken);
            if (schMgr != null) schoolId = schMgr.SchoolID;

            var token = _jwtTokenGenerator.GenerateToken(user, providerId, schoolId);
            var expiresIn = _jwtTokenGenerator.GetExpiryMinutes() * 60;
            var userDto = new UserDto(user.Id, user.Email, user.FullName, roleName, user.Phone, providerId);
            return Result<LoginResponse>.Success(new LoginResponse(
                token, expiresIn, userDto,
                RequiresTwoFactorSetup: true
            ));
        }

        // Case 3: No 2FA required (Parent without 2FA) → normal login
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Look up role-specific manager IDs
        Guid? pid = null;
        Guid? sid = null;
        var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, cancellationToken);
        if (providerMgr != null) pid = providerMgr.ProviderID;
        var schoolMgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, cancellationToken);
        if (schoolMgr != null) sid = schoolMgr.SchoolID;

        // Generate JWT token
        var jwtToken = _jwtTokenGenerator.GenerateToken(user, pid, sid);
        var jwtExpiry = _jwtTokenGenerator.GetExpiryMinutes() * 60;

        return Result<LoginResponse>.Success(new LoginResponse(
            jwtToken,
            jwtExpiry,
            new UserDto(user.Id, user.Email, user.FullName, roleName, user.Phone, pid)
        ));
    }
}
