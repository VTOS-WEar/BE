namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Response DTO for successful login.
/// If RequiresTwoFactor is true, AccessToken will be empty and TwoFactorToken will contain a temp token.
/// If RequiresTwoFactorSetup is true, user must set up 2FA before proceeding.
/// </summary>
public record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    UserDto User,
    bool RequiresTwoFactor = false,
    bool RequiresTwoFactorSetup = false,
    bool ShouldSetup2FA = false,
    string? TwoFactorToken = null
);

/// <summary>
/// User information returned in login response.
/// </summary>
public record UserDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string? Phone = null,
    Guid? ProviderId = null,
    bool? TwoFactorEnabled = null
);
