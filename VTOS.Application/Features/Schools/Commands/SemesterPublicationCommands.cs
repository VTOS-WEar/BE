using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

public record CreateSemesterPublicationCommand(
    Guid UserId,
    string Semester,
    string AcademicYear,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    string? Rules);

public interface ICreateSemesterPublicationCommandHandler
{
    Task<Result<SemesterPublicationDto>> HandleAsync(CreateSemesterPublicationCommand command, CancellationToken ct = default);
}

public record UpdateSemesterPublicationCommand(
    Guid UserId,
    Guid PublicationId,
    string? Semester,
    string? AcademicYear,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Description,
    string? Rules);

public interface IUpdateSemesterPublicationCommandHandler
{
    Task<Result<SemesterPublicationDto>> HandleAsync(UpdateSemesterPublicationCommand command, CancellationToken ct = default);
}

public record DeleteDraftPublicationCommand(Guid UserId, Guid PublicationId);

public interface IDeleteDraftPublicationCommandHandler
{
    Task<Result<string>> HandleAsync(DeleteDraftPublicationCommand command, CancellationToken ct = default);
}

public record PublishSemesterPublicationCommand(Guid UserId, Guid PublicationId);

public interface IPublishSemesterPublicationCommandHandler
{
    Task<Result<SemesterPublicationDto>> HandleAsync(PublishSemesterPublicationCommand command, CancellationToken ct = default);
}

public record CloseSemesterPublicationCommand(Guid UserId, Guid PublicationId);

public interface ICloseSemesterPublicationCommandHandler
{
    Task<Result<SemesterPublicationDto>> HandleAsync(CloseSemesterPublicationCommand command, CancellationToken ct = default);
}

public record AddOutfitsToPublicationCommand(
    Guid UserId,
    Guid PublicationId,
    IReadOnlyList<Guid> OutfitIds,
    string? Notes);

public interface IAddOutfitsToPublicationCommandHandler
{
    Task<Result<SemesterPublicationDetailDto>> HandleAsync(AddOutfitsToPublicationCommand command, CancellationToken ct = default);
}

public record RemoveOutfitFromPublicationCommand(Guid UserId, Guid PublicationId, Guid PublicationOutfitId);

public interface IRemoveOutfitFromPublicationCommandHandler
{
    Task<Result<SemesterPublicationDetailDto>> HandleAsync(RemoveOutfitFromPublicationCommand command, CancellationToken ct = default);
}

public record ApproveProviderCommand(Guid UserId, Guid PublicationId, Guid ProviderId, Guid? ContractId);

public interface IApproveProviderCommandHandler
{
    Task<Result<SemesterPublicationDetailDto>> HandleAsync(ApproveProviderCommand command, CancellationToken ct = default);
}

public record SuspendProviderCommand(Guid UserId, Guid PublicationId, Guid PublicationProviderId, string? Reason);

public interface ISuspendProviderCommandHandler
{
    Task<Result<SemesterPublicationDetailDto>> HandleAsync(SuspendProviderCommand command, CancellationToken ct = default);
}
