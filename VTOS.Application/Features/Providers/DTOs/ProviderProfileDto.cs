namespace VTOS.Application.Features.Providers.DTOs;

/// <summary>
/// Provider profile information returned to the frontend.
/// </summary>
public record ProviderProfileDto(
    Guid ProviderId,
    string ProviderName,
    string? ContactPersonName,
    string? Phone,
    string? Email,
    string? Address,
    string Status,
    /// <summary>User's 2FA status (from the Provider-manager user account)</summary>
    bool TwoFactorEnabled
);
