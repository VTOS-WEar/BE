using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that runs every 1 hour.
/// Finds orders in Pending status that are 18+ hours old (6h before 24h deadline)
/// and sends reminder emails to parents.
/// </summary>
public class PaymentDeadlineReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentDeadlineReminderJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public PaymentDeadlineReminderJob(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentDeadlineReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentDeadlineReminderJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PaymentDeadlineReminderJob.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Find Pending orders created 18+ hours ago (6h window before 24h auto-cancel)
        var cutoff = DateTime.UtcNow.AddHours(-18);

        var pendingOrders = await context.Orders
            .Where(o => o.OrderStatus == OrderStatus.Pending)
            .Where(o => o.OrderDate <= cutoff)
            .Include(o => o.ChildProfile)
                .ThenInclude(cp => cp.ParentUser)
            .Include(o => o.Campaign)
            .ToListAsync(ct);

        if (pendingOrders.Count == 0) return;

        // Get already-sent notification IDs to avoid duplicates
        var orderIds = pendingOrders.Select(o => o.Id).ToList();
        var alreadySentList = await context.NotificationLogs
            .Where(n => n.NotificationType == NotificationType.PaymentDeadlineReminder
                     && orderIds.Contains(n.ReferenceId))
            .Select(n => n.ReferenceId)
            .ToListAsync(ct);
        var alreadySent = new HashSet<Guid>(alreadySentList);

        var sentCount = 0;
        foreach (var order in pendingOrders)
        {
            if (alreadySent.Contains(order.Id)) continue;

            var parent = order.ChildProfile?.ParentUser;
            if (parent == null || string.IsNullOrEmpty(parent.Email)) continue;

            var orderCode = order.Id.ToString()[..8].ToUpper();
            var deadline = order.OrderDate.AddHours(24);
            var campaignName = order.Campaign?.CampaignName ?? "N/A";

            try
            {
                await emailService.SendPaymentDeadlineReminderAsync(
                    parent.Email,
                    parent.FullName,
                    orderCode,
                    order.TotalAmount,
                    deadline,
                    ct);

                context.NotificationLogs.Add(new NotificationLog
                {
                    UserId = parent.Id,
                    NotificationType = NotificationType.PaymentDeadlineReminder,
                    ReferenceId = order.Id,
                    Email = parent.Email,
                    SentAt = DateTime.UtcNow,
                    IsSuccess = true
                });
                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send payment reminder for Order {OrderId}.", order.Id);
                context.NotificationLogs.Add(new NotificationLog
                {
                    UserId = parent.Id,
                    NotificationType = NotificationType.PaymentDeadlineReminder,
                    ReferenceId = order.Id,
                    Email = parent.Email,
                    SentAt = DateTime.UtcNow,
                    IsSuccess = false,
                    ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 500)]
                });
            }
        }

        if (sentCount > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("PaymentDeadlineReminderJob: sent {Count} reminders.", sentCount);
        }
    }
}
