using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

public class DeleteStudentCommandHandler : IDeleteStudentCommandHandler
{
    private readonly IApplicationDbContext _db;
    public DeleteStudentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(DeleteStudentCommand cmd, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct);
        if (user == null)
            return Result<string>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var schoolId = schoolMgr.SchoolID;

        var child = await _db.ChildProfiles
            .FirstOrDefaultAsync(c => c.Id == cmd.StudentId && c.SchoolID == schoolId && !c.IsDeleted, ct);

        if (child == null)
            return Result<string>.Failure("Student not found.", "STUDENT_NOT_FOUND");

        _db.ChildProfiles.Remove(child);
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success("Student deleted successfully.");
    }
}
