using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Create a student (ChildProfile) under the current school.
/// </summary>
public class CreateStudentCommand
{
    public Guid UserId { get; }
    public string FullName { get; }
    public DateTime? DateOfBirth { get; }
    public Guid? ClassGroupId { get; }
    public string? Grade { get; }
    public string? Gender { get; }
    public string? ParentPhone { get; }

    public CreateStudentCommand(Guid userId, string fullName, DateTime? dateOfBirth,
        Guid? classGroupId,
        string? grade, string? gender, string? parentPhone)
    {
        UserId = userId;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        ClassGroupId = classGroupId;
        Grade = grade;
        Gender = gender;
        ParentPhone = parentPhone;
    }
}

public interface ICreateStudentCommandHandler
{
    Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand command, CancellationToken ct = default);
}
