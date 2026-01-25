namespace VTOS.Application.Features.Admin.Queries;
using VTOS.Application.Features.Admin.DTOs;

public record GetAllFeedbacksQuery();

public interface IGetAllFeedbacksQueryHandler
{
    Task<List<FeedbackDto>> HandleAsync(
        GetAllFeedbacksQuery query,
        CancellationToken cancellationToken);
}
