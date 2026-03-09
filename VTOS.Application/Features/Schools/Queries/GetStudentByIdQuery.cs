using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

public class GetStudentByIdQuery
{
    public Guid UserId { get; }
    public Guid StudentId { get; }

    public GetStudentByIdQuery(Guid userId, Guid studentId)
    {
        UserId = userId;
        StudentId = studentId;
    }
}

public interface IGetStudentByIdQueryHandler
{
    Task<Result<StudentDetailDto>> HandleAsync(GetStudentByIdQuery query, CancellationToken ct = default);
}
