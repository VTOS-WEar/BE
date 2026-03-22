using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Auth.Commands.TwoFactor;

// ── Disable2FA ──────────────────────────────────────────────────────
public record Disable2FACommand(Guid UserId, string Code);

public interface IDisable2FACommandHandler
{
    Task<Result<string>> HandleAsync(Disable2FACommand command, CancellationToken ct = default);
}

public class Disable2FACommandHandler : IDisable2FACommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ITotpService _totp;

    public Disable2FACommandHandler(IApplicationDbContext db, ITotpService totp)
    {
        _db = db; _totp = totp;
    }

    public async Task<Result<string>> HandleAsync(Disable2FACommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null) return Result<string>.Failure("User not found", "USER_NOT_FOUND");

        if (!user.IsTwoFactorEnabled)
            return Result<string>.Failure("2FA is not enabled", "2FA_NOT_ENABLED");

        // Verify TOTP code before disabling
        if (!_totp.VerifyCode(user.TwoFactorSecret!, command.Code))
            return Result<string>.Failure("Invalid verification code", "INVALID_2FA_CODE");

        user.IsTwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.RecoveryCodes = null;
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success("2FA has been disabled");
    }
}
