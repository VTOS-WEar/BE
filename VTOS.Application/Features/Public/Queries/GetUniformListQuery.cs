namespace VTOS.Application.Features.Public.Queries;

public record GetUniformListQuery(Guid SchoolId, int Page = 1, int PageSize = 10);
