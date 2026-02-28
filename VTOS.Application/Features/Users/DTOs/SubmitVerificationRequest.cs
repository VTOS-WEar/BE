using Microsoft.AspNetCore.Http;

namespace VTOS.Application.Features.Users.DTOs;

/// <summary>
/// Request DTO for updating user profile (avatar, name, phone).
/// </summary>
public record SubmitVerificationRequest(
    string? FullName,
    string? Phone,
    IFormFile? Avatar
);
