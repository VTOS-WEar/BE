using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Settings;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Services;

public class TryOnImageAccessService : ITryOnImageAccessService
{
    private readonly IApplicationDbContext _db;
    private readonly TryOnImageSecuritySettings _settings;
    private readonly byte[] _signingKey;

    public TryOnImageAccessService(
        IApplicationDbContext db,
        IOptions<TryOnImageSecuritySettings> settings,
        IConfiguration configuration)
    {
        _db = db;
        _settings = settings.Value;

        var key = _settings.SigningKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = configuration["JwtSettings:Secret"] ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Try-on image signing key is not configured.");
        }

        _signingKey = Encoding.UTF8.GetBytes(key);
    }

    public string? CreateImageUrl(TryOnHistory history, TryOnImageAssetKind assetKind)
    {
        var objectKey = GetObjectKey(history, assetKind);
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        var lifetime = TimeSpan.FromMinutes(Math.Max(1, _settings.TicketLifetimeMinutes));
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        var payload = new TryOnImageTicketPayload(
            Version: 1,
            TryOnId: history.Id,
            AssetKind: ToTicketKind(assetKind),
            Owner: OwnerFor(history),
            ExpiresAtUnix: expiresAt,
            Nonce: Guid.NewGuid().ToString("N"));

        return $"/api/tryon/images/{CreateToken(payload)}";
    }

    public async Task<Result<TryOnImageAccessGrant>> ValidateTicketAsync(
        string ticket,
        CancellationToken cancellationToken = default)
    {
        var payloadResult = TryReadToken(ticket);
        if (!payloadResult.IsSuccess)
        {
            return Result<TryOnImageAccessGrant>.Failure(
                payloadResult.Error ?? "Image ticket is invalid.",
                payloadResult.ErrorCode ?? "INVALID_IMAGE_TICKET");
        }

        var payload = payloadResult.Value!;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (payload.ExpiresAtUnix < now)
        {
            return Result<TryOnImageAccessGrant>.Failure("Image link has expired.", "IMAGE_TICKET_EXPIRED");
        }

        if (!TryParseKind(payload.AssetKind, out var assetKind))
        {
            return Result<TryOnImageAccessGrant>.Failure("Image ticket has an invalid asset kind.", "INVALID_IMAGE_TICKET");
        }

        var history = await _db.TryOnHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == payload.TryOnId, cancellationToken);

        if (history == null)
        {
            return Result<TryOnImageAccessGrant>.Failure("Try-on image was not found.", "TRYON_IMAGE_NOT_FOUND");
        }

        if (!FixedEquals(payload.Owner, OwnerFor(history)))
        {
            return Result<TryOnImageAccessGrant>.Failure("Image ticket owner does not match.", "INVALID_IMAGE_TICKET");
        }

        var objectKey = GetObjectKey(history, assetKind);
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return Result<TryOnImageAccessGrant>.Failure("Try-on image is not available.", "TRYON_IMAGE_NOT_FOUND");
        }

        return Result<TryOnImageAccessGrant>.Success(new TryOnImageAccessGrant(
            history.Id,
            assetKind,
            objectKey,
            GetContentType(history, assetKind),
            IsGuest(history)));
    }

    public async Task<Result<string>> CreateResultImageUrlAsync(
        Guid tryOnId,
        Guid? userId,
        string? guestSessionId,
        CancellationToken cancellationToken = default)
    {
        var history = await _db.TryOnHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tryOnId, cancellationToken);

        if (history == null)
        {
            return Result<string>.Failure("Try-on history was not found.", "TRYON_NOT_FOUND");
        }

        if (history.UserID.HasValue)
        {
            if (!userId.HasValue || userId.Value != history.UserID.Value)
            {
                return Result<string>.Failure("You are not allowed to access this try-on result.", "FORBIDDEN");
            }
        }
        else if (!FixedEquals(history.GuestSessionID ?? string.Empty, guestSessionId ?? string.Empty))
        {
            return Result<string>.Failure("Guest session does not match this try-on result.", "FORBIDDEN");
        }

        var url = CreateImageUrl(history, TryOnImageAssetKind.Result) ?? history.ResultPhotoURL;
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result<string>.Failure("Try-on result image is not available.", "TRYON_IMAGE_NOT_FOUND");
        }

        return Result<string>.Success(url);
    }

    private string CreateToken(TryOnImageTicketPayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        var signature = Sign(encodedPayload);
        return $"{encodedPayload}.{signature}";
    }

    private Result<TryOnImageTicketPayload> TryReadToken(string token)
    {
        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return Result<TryOnImageTicketPayload>.Failure("Image ticket is malformed.", "INVALID_IMAGE_TICKET");
        }

        var expectedSignature = Sign(parts[0]);
        if (!FixedEquals(expectedSignature, parts[1]))
        {
            return Result<TryOnImageTicketPayload>.Failure("Image ticket signature is invalid.", "INVALID_IMAGE_TICKET");
        }

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            var payload = JsonSerializer.Deserialize<TryOnImageTicketPayload>(json);
            if (payload == null || payload.Version != 1)
            {
                return Result<TryOnImageTicketPayload>.Failure("Image ticket payload is invalid.", "INVALID_IMAGE_TICKET");
            }

            return Result<TryOnImageTicketPayload>.Success(payload);
        }
        catch
        {
            return Result<TryOnImageTicketPayload>.Failure("Image ticket payload is invalid.", "INVALID_IMAGE_TICKET");
        }
    }

    private string Sign(string encodedPayload)
    {
        using var hmac = new HMACSHA256(_signingKey);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedPayload)));
    }

    private string OwnerFor(TryOnHistory history)
    {
        if (history.UserID.HasValue)
        {
            return $"user:{history.UserID.Value:N}";
        }

        var guestSessionId = history.GuestSessionID ?? string.Empty;
        using var hmac = new HMACSHA256(_signingKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(guestSessionId));
        return $"guest:{Base64UrlEncode(hash)}";
    }

    private static bool IsGuest(TryOnHistory history) => !history.UserID.HasValue;

    private static string? GetObjectKey(TryOnHistory history, TryOnImageAssetKind assetKind) =>
        assetKind == TryOnImageAssetKind.Result
            ? history.ResultPhotoObjectKey
            : history.UploadedPhotoObjectKey;

    private static string GetContentType(TryOnHistory history, TryOnImageAssetKind assetKind) =>
        assetKind == TryOnImageAssetKind.Result
            ? history.ResultPhotoContentType ?? "image/jpeg"
            : history.UploadedPhotoContentType ?? "image/jpeg";

    private static string ToTicketKind(TryOnImageAssetKind assetKind) =>
        assetKind == TryOnImageAssetKind.Result ? "result" : "uploaded";

    private static bool TryParseKind(string kind, out TryOnImageAssetKind assetKind)
    {
        if (kind.Equals("result", StringComparison.OrdinalIgnoreCase))
        {
            assetKind = TryOnImageAssetKind.Result;
            return true;
        }

        if (kind.Equals("uploaded", StringComparison.OrdinalIgnoreCase))
        {
            assetKind = TryOnImageAssetKind.Uploaded;
            return true;
        }

        assetKind = default;
        return false;
    }

    private static bool FixedEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }

    private record TryOnImageTicketPayload(
        int Version,
        Guid TryOnId,
        string AssetKind,
        string Owner,
        long ExpiresAtUnix,
        string Nonce);
}
