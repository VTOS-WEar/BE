namespace VTOS.Application.Features.Public.DTOs;

public record SizeChartMeasurementDto(
    string FieldKey,
    string DisplayName,
    string Unit,
    decimal? MinValue,
    decimal? MaxValue
);

public record SizeChartDetailDto(
    string SizeLabel,
    IEnumerable<SizeChartMeasurementDto> Measurements
);

public record SizeChartDto(
    Guid SizeChartId,
    string ChartName,
    string Unit,
    IEnumerable<SizeChartDetailDto> Details
);
