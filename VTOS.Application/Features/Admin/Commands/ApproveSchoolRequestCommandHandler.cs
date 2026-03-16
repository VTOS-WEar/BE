using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveSchoolRequestCommandHandler : IApproveSchoolRequestCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ApproveSchoolRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SchoolApprovalResponse>> HandleAsync(
        ApproveSchoolRequestCommand command,
        CancellationToken cancellationToken)
    {
        var school = await _context.Schools
            .FirstOrDefaultAsync(s => s.Id == command.SchoolId && !s.IsDeleted, cancellationToken);

        if (school == null)
            return Result<SchoolApprovalResponse>.Failure("School not found", "SCHOOL_NOT_FOUND");

        // Validate that school is in pending status
        if (school.VerificationStatus != VerificationStatus.Pending)
            return Result<SchoolApprovalResponse>.Failure("School verification status must be Pending", "INVALID_STATUS");

        if (command.Action.ToUpper() == "APPROVE")
        {
            school.VerificationStatus = VerificationStatus.Approved;
            school.Status = SchoolStatus.Active;
            school.RejectionReason = null; // Clear any previous rejection reason
            _context.Schools.Update(school);
        }
        else if (command.Action.ToUpper() == "REJECT")
        {
            // Validation: rejection reason is required
            if (string.IsNullOrWhiteSpace(command.RejectionReason))
                return Result<SchoolApprovalResponse>.Failure("Rejection reason is required when rejecting a school request", "REJECTION_REASON_REQUIRED");

            school.VerificationStatus = VerificationStatus.Rejected;
            school.Status = SchoolStatus.Rejected;
            school.RejectionReason = command.RejectionReason;
            _context.Schools.Update(school);
        }
        else
        {
            return Result<SchoolApprovalResponse>.Failure("Invalid action. Allowed values: APPROVE, REJECT", "INVALID_ACTION");
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        var response = new SchoolApprovalResponse
        {
            Id = school.Id,
            SchoolName = school.SchoolName,
            Status = school.Status.ToString(),
            VerificationStatus = school.VerificationStatus.ToString(),
            RejectionReason = school.RejectionReason,
            VerificationDocumentUrl = school.VerificationDocumentUrl,
            Message = $"School request {command.Action.ToLower()}ed successfully"
        };
        
        return Result<SchoolApprovalResponse>.Success(response);
    }
}
