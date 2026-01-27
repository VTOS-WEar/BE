namespace VTOS.Application.Features.Public.DTOs;

public record SchoolListResponse(
    IEnumerable<SchoolDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
