namespace VTOS.Application.Features.Admin.Commands;

public record ProviderApprovalResponse
{
    public Guid Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? VerificationDocumentUrl { get; set; }
    public string Message { get; set; } = string.Empty;
}
