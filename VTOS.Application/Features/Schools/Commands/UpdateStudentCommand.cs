using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

public class UpdateStudentCommand
{
    public Guid UserId { get; }
    public Guid StudentId { get; }
    public string? FullName { get; }
    public DateTime? DateOfBirth { get; }
    public string? Grade { get; }
    public string? Gender { get; }
    public int? HeightCm { get; }
    public float? WeightKg { get; }

    public UpdateStudentCommand(Guid userId, Guid studentId, string? fullName,
        DateTime? dateOfBirth, string? grade, string? gender, int? heightCm, float? weightKg)
    {
        UserId = userId;
        StudentId = studentId;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Grade = grade;
        Gender = gender;
        HeightCm = heightCm;
        WeightKg = weightKg;
    }
}

public interface IUpdateStudentCommandHandler
{
    Task<Result<StudentDetailDto>> HandleAsync(UpdateStudentCommand command, CancellationToken ct = default);
}
