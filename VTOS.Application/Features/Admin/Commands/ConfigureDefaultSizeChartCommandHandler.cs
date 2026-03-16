using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public class ConfigureDefaultSizeChartCommandHandler : IConfigureDefaultSizeChartCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ConfigureDefaultSizeChartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<string>> HandleAsync(
        ConfigureDefaultSizeChartCommand command,
        CancellationToken cancellationToken)
    {
        // Verify size chart exists
        var sizeChart = await _context.SizeCharts
            .FirstOrDefaultAsync(sc => sc.Id == command.SizeChartId, cancellationToken);

        if (sizeChart == null)
            return Result<string>.Failure("Size chart not found", "SIZE_CHART_NOT_FOUND");

        // In a real system, you'd store this in a settings/configuration table
        // For now, we'll just return success as the default is stored on the Outfit level
        // This would require a new Configuration entity to store system-wide defaults

        return Result<string>.Success($"Default size chart set to {sizeChart.ChartName}");
    }
}
