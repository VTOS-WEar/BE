namespace VTOS.Application.Features.Contracts.DTOs;

public record ContractDto(
    Guid ContractId,
    Guid SchoolId,
    Guid ProviderId,
    string ContractName,
    string Status,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    string? RejectionReason,
    string? SchoolName,
    string? ProviderName,
    DateTime ExpiresAt,
    List<ContractItemDto> Items
);

public record ContractItemDto(
    Guid ItemId,
    Guid OutfitId,
    string OutfitName,
    decimal PricePerUnit,
    int MinQuantity,
    int MaxQuantity
);

/// <summary>Request body for creating a contract.</summary>
public record CreateContractRequest(
    string ContractName,
    Guid ProviderId,
    DateTime ExpiresAt,
    List<CreateContractItemRequest> Items
);

public record CreateContractItemRequest(
    Guid OutfitId,
    decimal PricePerUnit,
    int MinQuantity,
    int MaxQuantity
);

/// <summary>Request body for rejecting a contract.</summary>
public record RejectContractRequest(string Reason);

