using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Commands;

public record UpsertParentAddressCommand(
    Guid ParentUserId,
    Guid? AddressId,
    string Label,
    string RecipientName,
    string RecipientPhone,
    string AddressLine,
    bool IsDefault);

public interface IUpsertParentAddressCommandHandler
{
    Task<Result<ParentAddressResponse>> HandleAsync(
        UpsertParentAddressCommand command,
        CancellationToken cancellationToken = default);
}
