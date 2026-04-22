using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

public record GetSemesterPublicationsQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record GetSemesterPublicationsResponse(
    IReadOnlyList<SemesterPublicationDto> Items,
    int Total,
    int Page,
    int PageSize);

public interface IGetSemesterPublicationsQueryHandler
{
    Task<Result<GetSemesterPublicationsResponse>> HandleAsync(GetSemesterPublicationsQuery query, CancellationToken ct = default);
}

public record GetSemesterPublicationDetailQuery(Guid UserId, Guid PublicationId);

public interface IGetSemesterPublicationDetailQueryHandler
{
    Task<Result<SemesterPublicationDetailDto>> HandleAsync(GetSemesterPublicationDetailQuery query, CancellationToken ct = default);
}

public record GetContractedOutfitSuggestionsQuery(Guid UserId);

public interface IGetContractedOutfitSuggestionsQueryHandler
{
    Task<Result<IReadOnlyList<ContractedOutfitSuggestionDto>>> HandleAsync(GetContractedOutfitSuggestionsQuery query, CancellationToken ct = default);
}

public record GetContractedProviderSuggestionsQuery(Guid UserId);

public interface IGetContractedProviderSuggestionsQueryHandler
{
    Task<Result<IReadOnlyList<ContractedProviderSuggestionDto>>> HandleAsync(GetContractedProviderSuggestionsQuery query, CancellationToken ct = default);
}
