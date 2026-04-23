namespace VTOS.Application.Features.Admin.Commands.DTOs;

public class SystemReportResponse
{
    public Guid ReportId { get; set; }
    public string ReportFrequency { get; set; } = string.Empty;
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int NewUsers { get; set; }
    public DateTime GeneratedAt { get; set; }
}
