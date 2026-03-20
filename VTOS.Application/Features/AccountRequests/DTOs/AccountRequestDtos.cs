namespace VTOS.Application.Features.AccountRequests.DTOs;

public record AccountRequestListItemDto(
    Guid Id,
    string OrganizationName,
    string ContactEmail,
    string ContactPhone,
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
    string Type,
    string? Description,
    string? Address,
    string Status,
    string? RejectionReason,
    Guid? ProcessedByUserId,
    string? ProcessedByName,
    Guid? CreatedUserId,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);

public record SubmitAccountRequestDto(
    string OrganizationName,
    string ContactEmail,
    string ContactPhone,
    int Type,         // 1=School, 2=Provider
    string? Description,
    string? Address
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
