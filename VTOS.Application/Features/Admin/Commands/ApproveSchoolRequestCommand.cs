using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ApproveSchoolRequestCommand(
    Guid SchoolId,
    string Action, // "APPROVE" or "REJECT"
    string? RejectionReason = null,
    string? AdminNote = null
);

public interface IApproveSchoolRequestCommandHandler
{
    Task<Result<SchoolApprovalResponse>> HandleAsync(
        ApproveSchoolRequestCommand command,
        CancellationToken cancellationToken);
}
