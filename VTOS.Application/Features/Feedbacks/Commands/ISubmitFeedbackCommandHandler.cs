using VTOS.Application.Common;
using VTOS.Application.Features.Feedbacks.DTOs;

namespace VTOS.Application.Features.Feedbacks.Commands;

public interface ISubmitFeedbackCommandHandler
{
    Task<Result<SubmitFeedbackResponse>> HandleAsync(SubmitFeedbackCommand command, CancellationToken cancellationToken);
}
