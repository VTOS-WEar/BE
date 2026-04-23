using VTOS.Application.Common;

namespace VTOS.Application.Features.Users.Commands;

public record DeleteParentAddressCommand(Guid ParentUserId, Guid AddressId);

public interface IDeleteParentAddressCommandHandler
{
    Task<Result> HandleAsync(DeleteParentAddressCommand command, CancellationToken cancellationToken = default);
}
