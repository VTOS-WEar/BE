namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for user registration (NO phone - collected later).
/// </summary>
public record RegisterRequest(
    string Email,
    string Password,
    string FullName
);
