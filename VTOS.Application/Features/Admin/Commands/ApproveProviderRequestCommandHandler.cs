using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveProviderRequestCommandHandler : IApproveProviderRequestCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ApproveProviderRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProviderApprovalResponse>> HandleAsync(
        ApproveProviderRequestCommand command,
        CancellationToken cancellationToken)
    {
        var provider = await _context.Providers
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId && !p.IsDeleted, cancellationToken);

        if (provider == null)
            return Result<ProviderApprovalResponse>.Failure("Provider not found", "PROVIDER_NOT_FOUND");

        // Validate that provider is in pending status
        if (provider.VerificationStatus != VerificationStatus.Pending)
            return Result<ProviderApprovalResponse>.Failure("Provider verification status must be Pending", "INVALID_STATUS");

        if (command.Action.ToUpper() == "APPROVE")
        {
            provider.VerificationStatus = VerificationStatus.Approved;
            provider.Status = ProviderStatus.Active;
            provider.RejectionReason = null; // Clear any previous rejection reason
            _context.Providers.Update(provider);
        }
        else if (command.Action.ToUpper() == "REJECT")
        {
            // Validation: rejection reason is required
            if (string.IsNullOrWhiteSpace(command.RejectionReason))
                return Result<ProviderApprovalResponse>.Failure("Rejection reason is required when rejecting a provider request", "REJECTION_REASON_REQUIRED");

            provider.VerificationStatus = VerificationStatus.Rejected;
            provider.Status = ProviderStatus.Rejected;
            provider.RejectionReason = command.RejectionReason;
            _context.Providers.Update(provider);
        }
        else
        {
            return Result<ProviderApprovalResponse>.Failure("Invalid action. Allowed values: APPROVE, REJECT", "INVALID_ACTION");
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        var response = new ProviderApprovalResponse
        {
            Id = provider.Id,
            ProviderName = provider.ProviderName,
            Email = provider.Email,
            Phone = provider.Phone,
            Status = provider.Status.ToString(),
            VerificationStatus = provider.VerificationStatus.ToString(),
            RejectionReason = provider.RejectionReason,
            VerificationDocumentUrl = provider.VerificationDocumentUrl,
            Message = $"Provider request {command.Action.ToLower()}ed successfully"
        };
        
        return Result<ProviderApprovalResponse>.Success(response);
    }
}
