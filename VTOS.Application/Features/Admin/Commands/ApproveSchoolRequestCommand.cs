using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ApproveSchoolRequestCommand(
    Guid SchoolId,
    string Action, // "APPROVE" or "REJECT"
    string? AdminNote = null
);

public interface IApproveSchoolRequestCommandHandler
{
    Task<Result<string>> HandleAsync(
        ApproveSchoolRequestCommand command,
        CancellationToken cancellationToken);
}
