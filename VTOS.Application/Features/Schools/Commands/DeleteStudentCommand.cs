using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

public class DeleteStudentCommand
{
    public Guid UserId { get; }
    public Guid StudentId { get; }

    public DeleteStudentCommand(Guid userId, Guid studentId)
    {
        UserId = userId;
        StudentId = studentId;
    }
}

public interface IDeleteStudentCommandHandler
{
    Task<Result<string>> HandleAsync(DeleteStudentCommand command, CancellationToken ct = default);
}
