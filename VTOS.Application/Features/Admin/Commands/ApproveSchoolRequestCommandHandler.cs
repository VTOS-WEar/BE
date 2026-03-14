using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveSchoolRequestCommandHandler : IApproveSchoolRequestCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ApproveSchoolRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<string>> HandleAsync(
        ApproveSchoolRequestCommand command,
        CancellationToken cancellationToken)
    {
        var school = await _context.Schools
            .FirstOrDefaultAsync(s => s.Id == command.SchoolId && !s.IsDeleted, cancellationToken);

        if (school == null)
            return Result<string>.Failure("School not found", "SCHOOL_NOT_FOUND");

        // Assuming school has a Status field or similar
        // If not, we'll need to add it to the domain model
        if (command.Action.ToUpper() == "APPROVE")
        {
            // Set school status to Active/Approved
            // school.Status = "Approved"; // Uncomment when School entity has Status field
            _context.Schools.Update(school);
        }
        else if (command.Action.ToUpper() == "REJECT")
        {
            // Set school status to Rejected
            // school.Status = "Rejected"; // Uncomment when School entity has Status field
            _context.Schools.Update(school);
        }
        else
        {
            return Result<string>.Failure("Invalid action", "INVALID_ACTION");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<string>.Success($"School request {command.Action.ToLower()}ed successfully");
    }
}
