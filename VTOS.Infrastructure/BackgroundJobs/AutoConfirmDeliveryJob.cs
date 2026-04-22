using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.BackgroundJobs;

public class AutoConfirmDeliveryJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoConfirmDeliveryJob> _logger;
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan AutoConfirmThreshold = TimeSpan.FromDays(7);

    public AutoConfirmDeliveryJob(IServiceProvider serviceProvider, ILogger<AutoConfirmDeliveryJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RunInterval, stoppingToken);
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoConfirmDeliveryJob cycle");
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var cutoff = DateTime.UtcNow.Subtract(AutoConfirmThreshold);

        var orders = await db.Orders
            .Where(o =>
                o.ProviderID != null &&
                o.SemesterPublicationID != null &&
                o.OrderStatus == OrderStatus.Shipped &&
                (o.UpdatedAt ?? o.OrderDate) <= cutoff)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
            return;

        foreach (var order in orders)
        {
            order.OrderStatus = OrderStatus.Delivered;
            order.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Auto-confirmed {Count} shipped direct orders.", orders.Count);
    }
}
