using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that runs every 6 hours.
/// Finds active campaigns ending within 24h and sends reminders to parents
/// whose children attend the campaign's school.
/// </summary>
public class CampaignDeadlineReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignDeadlineReminderJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public CampaignDeadlineReminderJob(
        IServiceScopeFactory scopeFactory,
        ILogger<CampaignDeadlineReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CampaignDeadlineReminderJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CampaignDeadlineReminderJob.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var deadline = now.AddHours(24);

        // Find active campaigns ending within 24h
        var expiringCampaigns = await context.Campaigns
            .Where(c => c.Status == CampaignStatus.Active)
            .Where(c => c.EndDate > now && c.EndDate <= deadline)
            .Include(c => c.School)
            .ToListAsync(ct);

        if (expiringCampaigns.Count == 0) return;

        var sentCount = 0;
        foreach (var campaign in expiringCampaigns)
        {
            // Find parents whose children attend this school
            var parentUsers = await context.ChildProfiles
                .Where(cp => cp.SchoolID == campaign.SchoolID && cp.ParentUserID != null)
                .Select(cp => cp.ParentUser)
                .Distinct()
                .ToListAsync(ct);

            if (parentUsers.Count == 0) continue;

            // Get already notified parent IDs for this campaign
            var alreadySentList = await context.NotificationLogs
                .Where(n => n.NotificationType == NotificationType.CampaignDeadlineReminder
                         && n.ReferenceId == campaign.Id)
                .Select(n => n.UserId)
                .ToListAsync(ct);
            var alreadySent = new HashSet<Guid>(alreadySentList);

            var schoolName = campaign.School?.SchoolName ?? "N/A";

            foreach (var parent in parentUsers)
            {
                if (parent == null || alreadySent.Contains(parent.Id)) continue;
                if (string.IsNullOrEmpty(parent.Email)) continue;

                try
                {
                    await emailService.SendCampaignDeadlineReminderAsync(
                        parent.Email,
                        parent.FullName,
                        campaign.CampaignName,
                        schoolName,
                        campaign.EndDate,
                        ct);

                    context.NotificationLogs.Add(new NotificationLog
                    {
                        UserId = parent.Id,
                        NotificationType = NotificationType.CampaignDeadlineReminder,
                        ReferenceId = campaign.Id,
                        Email = parent.Email,
                        SentAt = DateTime.UtcNow,
                        IsSuccess = true
                    });
                    sentCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send campaign reminder for Campaign {CampaignId} to {Email}.",
                        campaign.Id, parent.Email);
                    context.NotificationLogs.Add(new NotificationLog
                    {
                        UserId = parent.Id,
                        NotificationType = NotificationType.CampaignDeadlineReminder,
                        ReferenceId = campaign.Id,
                        Email = parent.Email,
                        SentAt = DateTime.UtcNow,
                        IsSuccess = false,
                        ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 500)]
                    });
                }
            }
        }

        if (sentCount > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("CampaignDeadlineReminderJob: sent {Count} reminders.", sentCount);
        }
    }
}
