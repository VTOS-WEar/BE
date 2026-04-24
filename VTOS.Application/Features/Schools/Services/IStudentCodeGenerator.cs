namespace VTOS.Application.Features.Schools.Services;

public interface IStudentCodeGenerator
{
    Task<string> GenerateAsync(
        Guid schoolId,
        string className,
        IEnumerable<string>? reservedCodes = null,
        CancellationToken ct = default);
}
