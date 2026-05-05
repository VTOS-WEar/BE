namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for user registration.
/// RoleName is optional: "Parent" (default) or "School".
/// </summary>
public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string TurnstileToken,
    string? RoleName = null,
    bool AcceptedTerms = false,
    string? TermsVersion = null
);
