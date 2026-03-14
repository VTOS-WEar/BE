using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveProviderRequestCommandHandler : IApproveProviderRequestCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ApproveProviderRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<string>> HandleAsync(
        ApproveProviderRequestCommand command,
        CancellationToken cancellationToken)
    {
        var provider = await _context.Providers
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId && !p.IsDeleted, cancellationToken);

        if (provider == null)
            return Result<string>.Failure("Provider not found", "PROVIDER_NOT_FOUND");

        if (string.IsNullOrEmpty(provider.Status))
            return Result<string>.Failure("Provider status is not pending", "INVALID_STATUS");

        if (command.Action.ToUpper() == "APPROVE")
        {
            provider.Status = "Active";
            _context.Providers.Update(provider);
        }
        else if (command.Action.ToUpper() == "REJECT")
        {
            provider.Status = "Rejected";
            _context.Providers.Update(provider);
        }
        else
        {
            return Result<string>.Failure("Invalid action", "INVALID_ACTION");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<string>.Success($"Provider request {command.Action.ToLower()}ed successfully");
    }
}
