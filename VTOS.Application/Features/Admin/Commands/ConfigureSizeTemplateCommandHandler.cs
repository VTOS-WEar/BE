using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Admin.Commands;

public class ConfigureSizeTemplateCommandHandler : IConfigureSizeTemplateCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ConfigureSizeTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> HandleAsync(
        ConfigureSizeTemplateCommand command,
        CancellationToken cancellationToken)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(command.ChartName))
            return Result<Guid>.Failure("Chart name is required", "INVALID_CHART_NAME");

        var sizeChart = new SizeChart
        {
            Id = Guid.NewGuid(),
            ChartName = command.ChartName,
            Description = command.Description,
            Unit = command.Unit,
            CreatedAt = DateTime.UtcNow
        };

        _context.SizeCharts.Add(sizeChart);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(sizeChart.Id);
    }
}
