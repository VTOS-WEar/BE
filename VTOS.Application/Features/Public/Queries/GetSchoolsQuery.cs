namespace VTOS.Application.Features.Public.Queries;

public record GetSchoolsQuery(
    string? Search,
    int Page = 1,
    int PageSize = 10
);
