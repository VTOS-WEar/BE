namespace VTOS.Application.Features.Schools.DTOs;

public record SemesterPublicationDto(
    Guid Id,
    Guid SchoolID,
    string Semester,
    string AcademicYear,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    string? Description,
    string? Rules,
    int OutfitCount,
    int ProviderCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record SemesterPublicationDetailDto(
    Guid Id,
    Guid SchoolID,
    string Semester,
    string AcademicYear,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    string? Description,
    string? Rules,
    IReadOnlyList<PublicationOutfitDto> Outfits,
    IReadOnlyList<PublicationProviderDto> Providers,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record PublicationOutfitDto(
    Guid Id,
    Guid OutfitID,
    string OutfitName,
    string? MainImageURL,
    decimal Price,
    string OutfitType,
    string? Notes,
    DateTime AddedAt);

public record PublicationProviderDto(
    Guid Id,
    Guid ProviderID,
    string ProviderName,
    string? ContactEmail,
    Guid? ContractID,
    string? ContractName,
    string Status,
    DateTime ApprovedAt,
    DateTime? SuspendedAt,
    string? SuspendReason);

public record ContractedOutfitSuggestionDto(
    Guid OutfitID,
    string OutfitName,
    string? MainImageURL,
    string OutfitType,
    string ContractName,
    Guid ContractID);

public record ContractedProviderSuggestionDto(
    Guid ProviderID,
    string ProviderName,
    string? ContactEmail,
    Guid ContractID,
    string ContractName,
    string ContractStatus);

public record CreateSemesterPublicationRequest(
    string Semester,
    string AcademicYear,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    string? Rules);

public record UpdateSemesterPublicationRequest(
    string? Semester,
    string? AcademicYear,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Description,
    string? Rules);

public record AddOutfitsRequest(IReadOnlyList<Guid> OutfitIds, string? Notes);

public record ApproveProviderRequest(Guid ProviderID, Guid? ContractID);

public record SuspendProviderRequest(string? Reason);
