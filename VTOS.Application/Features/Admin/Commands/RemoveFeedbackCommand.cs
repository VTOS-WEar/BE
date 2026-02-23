namespace VTOS.Application.Features.Admin.Commands;

public record RemoveFeedbackCommand(Guid FeedbackId);

public interface IRemoveFeedbackCommandHandler
{
    Task<bool> HandleAsync(
        RemoveFeedbackCommand command,
        CancellationToken cancellationToken);
}
