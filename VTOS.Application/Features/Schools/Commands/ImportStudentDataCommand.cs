using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-43: Import student data from CSV file.
/// </summary>
public class ImportStudentDataCommand
{
    public Guid UserId { get; }
    public Stream CsvStream { get; }

    public ImportStudentDataCommand(Guid userId, Stream csvStream)
    {
        UserId = userId;
        CsvStream = csvStream;
    }
}

public interface IImportStudentDataCommandHandler
{
    Task<Result<ImportStudentResultDto>> HandleAsync(ImportStudentDataCommand command, CancellationToken ct = default);
}
