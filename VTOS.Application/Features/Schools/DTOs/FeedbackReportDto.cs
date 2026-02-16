namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// DTO for school feedback report (UC-50).
/// </summary>
public class FeedbackReportDto
{
    public int TotalFeedbacks { get; set; }
    public double AvgRating { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new(); // key: rating (1-5), value: count
    public List<RecentFeedbackDto> RecentFeedbacks { get; set; } = new();
}

public class RecentFeedbackDto
{
    public Guid FeedbackId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string OutfitName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime Timestamp { get; set; }
}
