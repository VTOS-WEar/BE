namespace VTOS.Application.Features.Account.DTOs;

public record UpdateAccountEmailRequest(string Email);

public record UpdateAccountEmailResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string? Phone);
