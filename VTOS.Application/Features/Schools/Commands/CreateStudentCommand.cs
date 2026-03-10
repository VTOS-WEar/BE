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
    public string? Grade { get; }
    public string? Gender { get; }
    public string? ParentPhone { get; }
    public int? HeightCm { get; }
    public float? WeightKg { get; }

    public CreateStudentCommand(Guid userId, string fullName, DateTime? dateOfBirth,
        string? grade, string? gender, string? parentPhone, int? heightCm, float? weightKg)
    {
        UserId = userId;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Grade = grade;
        Gender = gender;
        ParentPhone = parentPhone;
        HeightCm = heightCm;
        WeightKg = weightKg;
    }
}

public interface ICreateStudentCommandHandler
{
    Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand command, CancellationToken ct = default);
}
