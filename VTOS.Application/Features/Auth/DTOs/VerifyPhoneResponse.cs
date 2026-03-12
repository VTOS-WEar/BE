namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Response DTO for phone verification.
/// Only confirms phone was saved — child linking is done via POST /api/users/me/find-children.
/// </summary>
public record VerifyPhoneResponse(
    string Phone,
    string Message
);

