namespace VTOS.Application.Features.Public.Queries;

/// <summary>
/// Unified search query across schools and uniforms.
/// No authentication required.
/// </summary>
public record PublicSearchQuery(
    string? Q = null,
    int Page = 1,
    int PageSize = 10
);
