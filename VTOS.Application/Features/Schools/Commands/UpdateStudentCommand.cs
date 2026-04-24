using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

public class UpdateStudentCommand
{
    public Guid UserId { get; }
    public Guid StudentId { get; }
    public string? FullName { get; }
    public DateTime? DateOfBirth { get; }
    public Guid? ClassGroupId { get; }
    public string? Grade { get; }
    public string? Gender { get; }
    public int? HeightCm { get; }
    public float? WeightKg { get; }
    public string? ParentPhone { get; }

    public UpdateStudentCommand(Guid userId, Guid studentId, string? fullName,
        DateTime? dateOfBirth, Guid? classGroupId, string? grade, string? gender, int? heightCm, float? weightKg,
        string? parentPhone = null)
    {
        UserId = userId;
        StudentId = studentId;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        ClassGroupId = classGroupId;
        Grade = grade;
        Gender = gender;
        HeightCm = heightCm;
        WeightKg = weightKg;
        ParentPhone = parentPhone;
    }
}

public interface IUpdateStudentCommandHandler
{
    Task<Result<StudentDetailDto>> HandleAsync(UpdateStudentCommand command, CancellationToken ct = default);
}
