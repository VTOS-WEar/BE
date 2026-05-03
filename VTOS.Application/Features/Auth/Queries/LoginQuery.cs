using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Queries;

/// <summary>
/// Query for user login.
/// </summary>
public record LoginQuery(
    string Email,
    string Password,
    string TurnstileToken
);

/// <summary>
/// Handler interface for LoginQuery.
/// </summary>
public interface ILoginQueryHandler
{
    Task<Result<LoginResponse>> HandleAsync(LoginQuery query, CancellationToken cancellationToken = default);
}
