using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;

namespace VTOS.Application.Features.Admin.Commands;

public class RemoveFeedbackCommandHandler : IRemoveFeedbackCommandHandler
{
    private readonly IApplicationDbContext _context;

    public RemoveFeedbackCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HandleAsync(
        RemoveFeedbackCommand command,
        CancellationToken cancellationToken)
    {
        var feedback = await _context.Feedbacks
            .FirstOrDefaultAsync(f => f.Id == command.FeedbackId, cancellationToken);

        if (feedback == null)
            return false;

        _context.Feedbacks.Remove(feedback);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
