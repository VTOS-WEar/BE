using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Admin.Commands.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public class GenerateSystemReportCommandHandler : IGenerateSystemReportCommandHandler
{
    private readonly IApplicationDbContext _context;

    public GenerateSystemReportCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SystemReportResponse>> HandleAsync(
        GenerateSystemReportCommand command,
        CancellationToken cancellationToken)
    {
        // Validation
        var validFrequencies = new[] { "Daily", "Weekly", "Monthly" };
        if (!validFrequencies.Contains(command.ReportFrequency))
            return Result<SystemReportResponse>.Failure("Invalid report frequency", "INVALID_FREQUENCY");

        try
        {
            // Calculate date range based on frequency
            var (dateFrom, dateTo) = command.ReportFrequency switch
            {
                "Daily" => (DateTime.UtcNow.AddDays(-1).Date, DateTime.UtcNow.Date),
                "Weekly" => (DateTime.UtcNow.AddDays(-7).Date, DateTime.UtcNow.Date),
                "Monthly" => (DateTime.UtcNow.AddMonths(-1).Date, DateTime.UtcNow.Date),
                _ => (DateTime.UtcNow.Date, DateTime.UtcNow.Date)
            };

            // Gather report data
            var ordersInPeriod = await _context.Orders
                .Where(o => o.CreatedAt >= dateFrom && o.CreatedAt <= dateTo)
                .CountAsync(cancellationToken);

            var revenueInPeriod = await _context.Orders
                .Where(o => o.CreatedAt >= dateFrom && o.CreatedAt <= dateTo && o.OrderStatus == OrderStatus.Delivered)
                .SumAsync(o => o.TotalAmount, cancellationToken);

            var completedOrders = await _context.Orders
                .Where(o => o.CreatedAt >= dateFrom && o.CreatedAt <= dateTo && o.OrderStatus == OrderStatus.Delivered)
                .CountAsync(cancellationToken);

            var newUsers = await _context.Users
                .Where(u => u.CreatedAt >= dateFrom && u.CreatedAt <= dateTo)
                .CountAsync(cancellationToken);

            // In a real system, you would:
            // 1. Store this in a SystemReport or ReportLog table
            // 2. Send email notifications to admin
            // 3. Archive the report

            var reportId = Guid.NewGuid();
            var response = new SystemReportResponse
            {
                ReportId = reportId,
                ReportFrequency = command.ReportFrequency,
                PeriodFrom = dateFrom,
                PeriodTo = dateTo,
                TotalOrders = ordersInPeriod,
                CompletedOrders = completedOrders,
                TotalRevenue = revenueInPeriod,
                NewUsers = newUsers,
                GeneratedAt = DateTime.UtcNow
            };

            return Result<SystemReportResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<SystemReportResponse>.Failure($"Report generation failed: {ex.Message}", "GENERATION_ERROR");
        }
    }
}
