namespace VTOS.Application.Features.Public.DTOs;

public record SizeChartDetailDto(
    string SizeLabel,
    decimal? ChestMin,
    decimal? ChestMax,
    decimal? WaistMin,
    decimal? WaistMax,
    decimal? HeightMin,
    decimal? HeightMax
);

public record SizeChartDto(
    Guid SizeChartId,
    string ChartName,
    string Unit,
    IEnumerable<SizeChartDetailDto> Details
);
