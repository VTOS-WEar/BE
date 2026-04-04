using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Feedbacks.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Feedbacks.Commands;

public class SubmitFeedbackCommandHandler : ISubmitFeedbackCommandHandler
{
    private readonly IApplicationDbContext _context;

    public SubmitFeedbackCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SubmitFeedbackResponse>> HandleAsync(
        SubmitFeedbackCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate rating
        if (command.Rating < 1 || command.Rating > 5)
        {
            return Result<SubmitFeedbackResponse>.Failure(
                "Rating must be between 1 and 5",
                "INVALID_RATING");
        }

        // Check if user already has feedback for this product variant in this campaign
        var existingFeedback = await _context.Feedbacks
            .FirstOrDefaultAsync(
                f => f.UserID == command.UserId 
                    && f.ProductVariantID == command.ProductVariantId
                    && f.CampaignID == command.CampaignId,
                cancellationToken);

        if (existingFeedback != null)
        {
            return Result<SubmitFeedbackResponse>.Failure(
                "You have already submitted feedback for this outfit",
                "FEEDBACK_ALREADY_EXISTS");
        }

        // Create feedback
        var feedback = new Feedback
        {
            UserID = command.UserId,
            ProductVariantID = command.ProductVariantId,
            CampaignID = command.CampaignId,
            Rating = command.Rating,
            Comment = command.Comment?.Trim(),
            Timestamp = DateTime.UtcNow,
            ModerationStatus = ModerationStatus.Pending
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<SubmitFeedbackResponse>.Success(
            new SubmitFeedbackResponse(
                feedback.Id,
                command.ProductVariantId,
                command.CampaignId,
                feedback.Rating,
                feedback.Comment,
                feedback.Timestamp
            ));
    }
}
