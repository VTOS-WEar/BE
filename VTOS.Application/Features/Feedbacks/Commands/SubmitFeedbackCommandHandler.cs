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

        // Verify OrderItem exists and belongs to this user's order
        var orderItem = await _context.OrderItems
            .Include(oi => oi.Order)
                .ThenInclude(o => o.ChildProfile)
            .FirstOrDefaultAsync(oi => oi.Id == command.OrderItemId, cancellationToken);

        if (orderItem == null)
        {
            return Result<SubmitFeedbackResponse>.Failure(
                "Order item not found",
                "ORDER_ITEM_NOT_FOUND");
        }

        if (orderItem.Order.ChildProfile.ParentUserID != command.UserId)
        {
            return Result<SubmitFeedbackResponse>.Failure(
                "You are not authorized to submit feedback for this order",
                "UNAUTHORIZED_ORDER_ACCESS");
        }

        // Check if user already has feedback for this order item
        var existingFeedback = await _context.Feedbacks
            .FirstOrDefaultAsync(
                f => f.UserID == command.UserId 
                    && f.OrderItemID == command.OrderItemId,
                cancellationToken);

        if (existingFeedback != null)
        {
            return Result<SubmitFeedbackResponse>.Failure(
                "You have already submitted feedback for this item",
                "FEEDBACK_ALREADY_EXISTS");
        }

        // Create feedback
        var feedback = new Feedback
        {
            UserID = command.UserId,
            OrderItemID = command.OrderItemId,
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
                command.OrderItemId,
                feedback.Rating,
                feedback.Comment,
                feedback.Timestamp
            ));
    }
}
