using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Handles Google OAuth login:
/// 1. Validates Google ID token
/// 2. Finds or creates user
/// 3. Returns JWT (same format as normal login)
/// </summary>
public interface IGoogleLoginCommandHandler
{
    Task<Result<LoginResponse>> HandleAsync(GoogleLoginCommand command, CancellationToken ct = default);
}

public class GoogleLoginCommandHandler : IGoogleLoginCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public GoogleLoginCommandHandler(
        IApplicationDbContext context,
        IGoogleTokenValidator googleValidator,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _googleValidator = googleValidator;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResponse>> HandleAsync(
        GoogleLoginCommand command,
        CancellationToken ct = default)
    {
        // 1. Validate Google ID token
        var googleUser = await _googleValidator.ValidateAsync(command.IdToken);
        if (googleUser == null)
        {
            return Result<LoginResponse>.Failure(
                "Invalid Google token",
                "INVALID_GOOGLE_TOKEN");
        }

        // 2. Find user by GoogleId or Email
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.GoogleId == googleUser.Sub, ct);

        if (user == null)
        {
            // Try to find by email (link existing Local account)
            user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == googleUser.Email, ct);

            if (user != null)
            {
                // Link Google to existing account
                user.GoogleId = googleUser.Sub;
                user.AuthProvider = "Google";
                if (!user.IsActive) user.IsActive = true; // Auto-verify email
                if (string.IsNullOrEmpty(user.Avatar) && !string.IsNullOrEmpty(googleUser.Picture))
                    user.Avatar = googleUser.Picture;
            }
            else
            {
                // Create new Parent user
                var parentRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Parent", ct);

                if (parentRole == null)
                {
                    parentRole = new Role
                    {
                        Id = Guid.NewGuid(),
                        RoleName = "Parent",
                        Description = "Parent user role",
                        IsSystemRole = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Roles.Add(parentRole);
                }

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = googleUser.Email,
                    PasswordHash = string.Empty, // No password for Google users
                    FullName = googleUser.Name,
                    Avatar = googleUser.Picture ?? string.Empty,
                    GoogleId = googleUser.Sub,
                    AuthProvider = "Google",
                    RoleID = parentRole.Id,
                    IsActive = true, // Auto-verified via Google
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);

                // Create ParentProfile
                _context.ParentProfiles.Add(new ParentProfile
                {
                    Id = Guid.NewGuid(),
                    UserID = user.Id,
                    Gender = Domain.Enums.Gender.Other
                });

                // Need to save + reload to get Role nav property
                await _context.SaveChangesAsync(ct);

                user = await _context.Users
                    .Include(u => u.Role)
                    .FirstAsync(u => u.Id == user.Id, ct);
            }
        }

        // 3. Check if account is disabled
        if (user.IsDeleted)
        {
            return Result<LoginResponse>.Failure(
                "Account is disabled",
                "ACCOUNT_DISABLED");
        }

        // 4. Update last login
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // 5. Look up role-specific manager IDs
        Guid? providerId = null;
        Guid? schoolId = null;
        var providerMgr = await _context.ProviderManagers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr != null) providerId = providerMgr.ProviderID;

        var schoolMgr = await _context.SchoolManagers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr != null) schoolId = schoolMgr.SchoolID;

        // 6. Generate JWT (skip 2FA for Google login)
        var roleName = user.Role.RoleName;
        var token = _jwtTokenGenerator.GenerateToken(user, providerId, schoolId);
        var expiresIn = _jwtTokenGenerator.GetExpiryMinutes() * 60;

        return Result<LoginResponse>.Success(new LoginResponse(
            token,
            expiresIn,
            new UserDto(user.Id, user.Email, user.FullName, roleName, user.Phone, providerId, user.IsTwoFactorEnabled)
        ));
    }
}
