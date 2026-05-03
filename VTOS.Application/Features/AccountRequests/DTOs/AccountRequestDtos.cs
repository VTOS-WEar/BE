namespace VTOS.Application.Features.AccountRequests.DTOs;

public record AccountRequestListItemDto(
    Guid Id,
    string OrganizationName,
    string ContactEmail,
    string ContactPhone,
    string? ContactPersonName,
    string Type,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);

public record AccountRequestDetailDto(
    Guid Id,
    string OrganizationName,
    string ContactEmail,
    string ContactPhone,
    string? ContactPersonName,
    string Type,
    string? Description,
    string? Address,
    string Status,
    string? RejectionReason,
    Guid? ProcessedByUserId,
    string? ProcessedByName,
    Guid? CreatedUserId,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    DateTime? TermsAcceptedAt,
    string? TermsVersion
);

public record SubmitAccountRequestDto(
    string OrganizationName,
    string ContactEmail,
    string ContactPhone,
    string? ContactPersonName,
    int Type,         // 1=School, 2=Provider
    string? Description,
    string? Address,
    bool AcceptedTerms = false,
    string? TermsVersion = null
);

public record CreateAccountForRequestDto(
    string Email,
    string FullName,
    string? Phone
);

public record RejectAccountRequestDto(
    string Reason
);

public record AccountRequestListResponse(
    List<AccountRequestListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
