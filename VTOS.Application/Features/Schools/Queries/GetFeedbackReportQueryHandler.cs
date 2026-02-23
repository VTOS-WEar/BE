using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-50: View feedback reports for the school.
/// Aggregates feedbacks on outfits belonging to the school.
/// </summary>
public class GetFeedbackReportQueryHandler : IGetFeedbackReportQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetFeedbackReportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<FeedbackReportDto>> HandleAsync(GetFeedbackReportQuery query, CancellationToken ct = default)
    {
        // Get outfit IDs belonging to this school
        var schoolOutfitIds = await _db.Outfits
            .AsNoTracking()
            .Where(o => o.SchoolID == query.SchoolId)
            .Select(o => o.Id)
            .ToListAsync(ct);

        if (!schoolOutfitIds.Any())
        {
            return Result<FeedbackReportDto>.Success(new FeedbackReportDto
            {
                TotalFeedbacks = 0,
                AvgRating = 0,
                RatingDistribution = new Dictionary<int, int>
                {
                    { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
                }
            });
        }

        // Get feedbacks for school's outfits
        var feedbacksQuery = _db.Feedbacks
            .AsNoTracking()
            .Include(f => f.User)
            .Include(f => f.Outfit)
            .Where(f => schoolOutfitIds.Contains(f.OutfitID));

        // Apply date filters
        if (query.FromDate.HasValue)
            feedbacksQuery = feedbacksQuery.Where(f => f.Timestamp >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            feedbacksQuery = feedbacksQuery.Where(f => f.Timestamp <= query.ToDate.Value);

        var feedbacks = await feedbacksQuery.ToListAsync(ct);

        var totalFeedbacks = feedbacks.Count;
        var avgRating = totalFeedbacks > 0 ? feedbacks.Average(f => f.Rating) : 0;

        // Rating distribution (1-5)
        var ratingDistribution = new Dictionary<int, int>
        {
            { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
        };
        foreach (var f in feedbacks)
        {
            if (ratingDistribution.ContainsKey(f.Rating))
                ratingDistribution[f.Rating]++;
        }

        // Recent feedbacks (latest 20)
        var recentFeedbacks = feedbacks
            .OrderByDescending(f => f.Timestamp)
            .Take(20)
            .Select(f => new RecentFeedbackDto
            {
                FeedbackId = f.Id,
                UserName = f.User.FullName,
                OutfitName = f.Outfit.OutfitName,
                Rating = f.Rating,
                Comment = f.Comment,
                Timestamp = f.Timestamp
            })
            .ToList();

        return Result<FeedbackReportDto>.Success(new FeedbackReportDto
        {
            TotalFeedbacks = totalFeedbacks,
            AvgRating = avgRating,
            RatingDistribution = ratingDistribution,
            RecentFeedbacks = recentFeedbacks
        });
    }
}
