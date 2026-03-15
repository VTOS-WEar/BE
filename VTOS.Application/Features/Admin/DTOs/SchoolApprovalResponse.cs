namespace VTOS.Application.Features.Admin.Commands;

public record SchoolApprovalResponse
{
    public Guid Id { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? VerificationDocumentUrl { get; set; }
    public string Message { get; set; } = string.Empty;
}
