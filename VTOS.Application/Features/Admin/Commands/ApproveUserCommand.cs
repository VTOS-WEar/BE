namespace VTOS.Application.Features.Admin.Commands;

public record ApproveUserCommand(Guid UserId);

public interface IApproveUserCommandHandler
{
    Task<bool> HandleAsync(
        ApproveUserCommand command,
        CancellationToken cancellationToken);
}
