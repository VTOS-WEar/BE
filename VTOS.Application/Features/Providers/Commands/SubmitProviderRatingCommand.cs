using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

public record SubmitProviderRatingCommand(Guid ParentUserId, Guid OrderId, int Rating, string? Comment);

public interface ISubmitProviderRatingCommandHandler
{
    Task<Result<SubmitProviderRatingResponse>> HandleAsync(SubmitProviderRatingCommand command, CancellationToken cancellationToken = default);
}

public class SubmitProviderRatingCommandHandler : ISubmitProviderRatingCommandHandler
{
    private readonly IApplicationDbContext _context;

    public SubmitProviderRatingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SubmitProviderRatingResponse>> HandleAsync(SubmitProviderRatingCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Rating < 1 || command.Rating > 5)
        {
            return Result<SubmitProviderRatingResponse>.Failure("Rating must be between 1 and 5.", "INVALID_RATING");
        }

        if (!string.IsNullOrWhiteSpace(command.Comment) && command.Comment.Length > 1000)
        {
            return Result<SubmitProviderRatingResponse>.Failure("Comment cannot exceed 1000 characters.", "COMMENT_TOO_LONG");
        }

        var order = await _context.Orders
            .Include(o => o.ChildProfile)
            .Include(o => o.Provider)
            .FirstOrDefaultAsync(
                o => o.Id == command.OrderId
                    && o.ProviderID != null
                    && o.SemesterPublicationID != null,
                cancellationToken);

        if (order == null)
        {
            return Result<SubmitProviderRatingResponse>.Failure("Marketplace order not found.", "ORDER_NOT_FOUND");
        }

        if (order.ChildProfile.ParentUserID != command.ParentUserId)
        {
            return Result<SubmitProviderRatingResponse>.Failure("You are not authorized to rate this order.", "UNAUTHORIZED_ORDER_ACCESS");
        }

        if (order.OrderStatus != OrderStatus.Delivered)
        {
            return Result<SubmitProviderRatingResponse>.Failure("Only delivered orders can be rated.", "ORDER_NOT_DELIVERED");
        }

        var existing = await _context.ProviderRatings
            .AnyAsync(x => x.OrderID == command.OrderId && x.ParentUserID == command.ParentUserId, cancellationToken);

        if (existing)
        {
            return Result<SubmitProviderRatingResponse>.Failure("Provider has already been rated for this order.", "PROVIDER_ALREADY_RATED");
        }

        var rating = new ProviderRating
        {
            ProviderID = order.ProviderID!.Value,
            OrderID = order.Id,
            ParentUserID = command.ParentUserId,
            Rating = command.Rating,
            Comment = command.Comment?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProviderRatings.Add(rating);
        await _context.SaveChangesAsync(cancellationToken);

        await RecalculateProviderAggregatesAsync(order.ProviderID.Value, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<SubmitProviderRatingResponse>.Success(new SubmitProviderRatingResponse(
            rating.Id,
            rating.OrderID,
            rating.ProviderID,
            rating.Rating,
            rating.Comment,
            rating.CreatedAt));
    }

    private async Task RecalculateProviderAggregatesAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var provider = await _context.Providers.FirstAsync(x => x.Id == providerId, cancellationToken);

        var ratings = await _context.ProviderRatings
            .Where(x => x.ProviderID == providerId)
            .ToListAsync(cancellationToken);

        provider.TotalRatings = ratings.Count;
        provider.AverageRating = ratings.Count == 0 ? 0m : Math.Round((decimal)ratings.Average(x => x.Rating), 2);
        provider.TotalCompletedOrders = await _context.Orders
            .CountAsync(
                x => x.ProviderID == providerId
                    && x.SemesterPublicationID != null
                    && x.OrderStatus == OrderStatus.Delivered,
                cancellationToken);
    }
}
