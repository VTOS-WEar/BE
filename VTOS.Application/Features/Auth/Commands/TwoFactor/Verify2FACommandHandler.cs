using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Commands.TwoFactor;

// ── Verify2FA (Login Step 2) ────────────────────────────────────────
public record Verify2FACommand(string TwoFactorToken, string Code);

public interface IVerify2FACommandHandler
{
    Task<Result<LoginResponse>> HandleAsync(Verify2FACommand command, CancellationToken ct = default);
}

public class Verify2FACommandHandler : IVerify2FACommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ITotpService _totp;

    public Verify2FACommandHandler(IApplicationDbContext db, IJwtTokenGenerator jwt, ITotpService totp)
    {
        _db = db; _jwt = jwt; _totp = totp;
    }

    public async Task<Result<LoginResponse>> HandleAsync(Verify2FACommand command, CancellationToken ct = default)
    {
        // Validate the temp 2FA token
        var userId = _jwt.ValidateTwoFactorToken(command.TwoFactorToken);
        if (userId == null)
            return Result<LoginResponse>.Failure("Invalid or expired 2FA token. Please login again.", "INVALID_2FA_TOKEN");

        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user == null)
            return Result<LoginResponse>.Failure("User not found", "USER_NOT_FOUND");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return Result<LoginResponse>.Failure("2FA is not configured", "2FA_NOT_CONFIGURED");

        // Try TOTP code first
        bool isValid = _totp.VerifyCode(user.TwoFactorSecret, command.Code);

        // If TOTP failed, try recovery code
        if (!isValid && !string.IsNullOrEmpty(user.RecoveryCodes))
        {
            var (recoveryValid, updatedCodes) = _totp.ValidateRecoveryCode(command.Code, user.RecoveryCodes);
            if (recoveryValid)
            {
                isValid = true;
                user.RecoveryCodes = updatedCodes;
            }
        }

        if (!isValid)
            return Result<LoginResponse>.Failure("Invalid verification code", "INVALID_2FA_CODE");

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Look up role-specific manager IDs
        Guid? providerId = null;
        Guid? schoolId = null;
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr != null) providerId = providerMgr.ProviderID;
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr != null) schoolId = schoolMgr.SchoolID;

        // Generate real JWT
        var token = _jwt.GenerateToken(user, providerId, schoolId);
        var expiresIn = _jwt.GetExpiryMinutes() * 60;

        return Result<LoginResponse>.Success(new LoginResponse(
            token,
            expiresIn,
            new UserDto(user.Id, user.Email, user.FullName, user.Role.RoleName, user.Phone, providerId)
        ));
    }
}
