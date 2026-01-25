using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public class GetAllFeedbacksQueryHandler : IGetAllFeedbacksQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAllFeedbacksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FeedbackDto>> HandleAsync(
        GetAllFeedbacksQuery query,
        CancellationToken cancellationToken)
    {
        return await _context.Feedbacks
            .Include(f => f.User)
            .OrderByDescending(f => f.Timestamp) // ✅ đúng field
            .Select(f => new FeedbackDto(
                f.Id,
                f.User.Email,
                f.Comment,
                f.Rating,
                f.Timestamp,
                f.ModerationStatus.ToString()
            ))
            .ToListAsync(cancellationToken);
    }
}
