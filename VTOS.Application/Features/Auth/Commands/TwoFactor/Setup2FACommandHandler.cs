using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Auth.Commands.TwoFactor;

// ── Setup2FA ────────────────────────────────────────────────────────
public record Setup2FACommand(Guid UserId);
public record Setup2FAResponse(string QrCodeUri, string ManualKey);

public interface ISetup2FACommandHandler
{
    Task<Result<Setup2FAResponse>> HandleAsync(Setup2FACommand command, CancellationToken ct = default);
}

public class Setup2FACommandHandler : ISetup2FACommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ITotpService _totp;

    public Setup2FACommandHandler(IApplicationDbContext db, ITotpService totp)
    {
        _db = db; _totp = totp;
    }

    public async Task<Result<Setup2FAResponse>> HandleAsync(Setup2FACommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null) return Result<Setup2FAResponse>.Failure("User not found", "USER_NOT_FOUND");

        if (user.IsTwoFactorEnabled)
            return Result<Setup2FAResponse>.Failure("2FA is already enabled", "2FA_ALREADY_ENABLED");

        // If a pending secret already exists (user scanned but hasn't confirmed yet),
        // return the existing secret so 2fa.live / authenticator stays in sync.
        // Re-generating would overwrite the secret, invalidating the QR code already scanned.
        var secret = user.TwoFactorSecret;
        if (string.IsNullOrEmpty(secret))
        {
            secret = _totp.GenerateSecret();
            user.TwoFactorSecret = secret;
            await _db.SaveChangesAsync(ct);
        }

        var qrUri = _totp.GetQrCodeUri(secret, user.Email);
        return Result<Setup2FAResponse>.Success(new Setup2FAResponse(qrUri, secret));
    }
}
