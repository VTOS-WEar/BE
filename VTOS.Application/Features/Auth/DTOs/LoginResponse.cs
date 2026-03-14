namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Response DTO for successful login.
/// </summary>
public record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    UserDto User
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
    Guid? ProviderId = null
);
