using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Auth.Commands.TwoFactor;

// ── Confirm2FA ──────────────────────────────────────────────────────
public record Confirm2FACommand(Guid UserId, string Code);
public record Confirm2FAResponse(List<string> RecoveryCodes);

public interface IConfirm2FACommandHandler
{
    Task<Result<Confirm2FAResponse>> HandleAsync(Confirm2FACommand command, CancellationToken ct = default);
}

public class Confirm2FACommandHandler : IConfirm2FACommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ITotpService _totp;

    public Confirm2FACommandHandler(IApplicationDbContext db, ITotpService totp)
    {
        _db = db; _totp = totp;
    }

    public async Task<Result<Confirm2FAResponse>> HandleAsync(Confirm2FACommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null) return Result<Confirm2FAResponse>.Failure("User not found", "USER_NOT_FOUND");

        if (user.IsTwoFactorEnabled)
            return Result<Confirm2FAResponse>.Failure("2FA is already enabled", "2FA_ALREADY_ENABLED");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return Result<Confirm2FAResponse>.Failure("Please initiate 2FA setup first", "2FA_NOT_SETUP");

        // Verify the TOTP code from their authenticator app
        if (!_totp.VerifyCode(user.TwoFactorSecret, command.Code))
            return Result<Confirm2FAResponse>.Failure("Invalid verification code", "INVALID_2FA_CODE");

        // Enable 2FA and generate recovery codes
        user.IsTwoFactorEnabled = true;
        var (plainCodes, hashedJson) = _totp.GenerateRecoveryCodes();
        user.RecoveryCodes = hashedJson;
        await _db.SaveChangesAsync(ct);

        return Result<Confirm2FAResponse>.Success(new Confirm2FAResponse(plainCodes));
    }
}
