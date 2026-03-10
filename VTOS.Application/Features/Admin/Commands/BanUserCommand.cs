namespace VTOS.Application.Features.Admin.Commands;

public record BanUserCommand(Guid UserId);

public interface IBanUserCommandHandler
{
    Task<bool> HandleAsync(
        BanUserCommand command,
        CancellationToken cancellationToken);
}