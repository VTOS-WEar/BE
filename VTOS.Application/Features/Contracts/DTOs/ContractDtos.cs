namespace VTOS.Application.Features.Contracts.DTOs;

public record ContractDto(
    Guid ContractId,
    Guid SchoolId,
    Guid ProviderId,
    string ContractName,
    string ContractNumber,
    string Status,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    string? RejectionReason,
    DateTime ExpiresAt,

    // Party names
    string? SchoolName,
    string? ProviderName,

    // School extended info (for contract template auto-fill)
    string? SchoolAddress,
    string? SchoolTaxCode,
    string? SchoolRepName,
    string? SchoolRepTitle,
    string? SchoolPhone,

    // Provider extended info (for contract template auto-fill)
    string? ProviderAddress,
    string? ProviderTaxCode,
    string? ProviderRepName,
    string? ProviderRepTitle,
    string? ProviderPhone,
    string? ProviderEmail,

    // Digital signatures
    string? SchoolSignature,
    DateTime? SchoolSignedAt,
    string? ProviderSignature,
    DateTime? ProviderSignedAt,

    // Masked contact of the CURRENT viewer (for OTP display)
    string? ViewerMaskedContact,

    // Generated contract PDF URL (served from /contracts/{id}.pdf)
    string? ContractPdfUrl,

    List<ContractItemDto> Items
);

public record ContractItemDto(
    Guid ItemId,
    Guid OutfitId,
    string OutfitName,
    string? MainImageURL,
    decimal? PricePerUnit,
    int? MinQuantity,
    int? MaxQuantity
);

/// <summary>Request body for creating a contract (School).</summary>
public record CreateContractRequest(
    string ContractName,
    Guid ProviderId,
    DateTime ExpiresAt,
    List<CreateContractItemRequest> Items
);

public record CreateContractItemRequest(
    Guid OutfitId
);

public record UpdateContractPricingRequest(
    List<UpdateContractPricingItemRequest> Items
);

public record UpdateContractPricingItemRequest(
    Guid ItemId,
    decimal PricePerUnit
);

/// <summary>Request body for rejecting a contract (Provider).</summary>
public record RejectContractRequest(string Reason);

/// <summary>Request body for signing a contract (School or Provider).</summary>
public record SignContractRequest(
    /// <summary>Base64-encoded PNG of the signature image.</summary>
    string SignatureData,
    /// <summary>The 6-digit OTP received by email.</summary>
    string OTPCode,
    /// <summary>Base64-encoded PDF of the full contract (generated client-side). Optional.</summary>
    string? PdfBase64 = null
);
