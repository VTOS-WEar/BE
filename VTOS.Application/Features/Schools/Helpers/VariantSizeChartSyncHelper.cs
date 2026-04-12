using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Helpers;

internal static class VariantSizeChartSyncHelper
{
    public static List<VariantMeasurementInputDto> NormalizeInputs(IEnumerable<VariantMeasurementInputDto>? inputs) => Normalize(inputs);

    public static List<VariantMeasurementDto> ToDtos(SizeChartDetail? detail)
    {
        return detail == null ? new List<VariantMeasurementDto>() : ToDtos(detail.Measurements);
    }

    public static List<VariantMeasurementDto> ToDtos(IEnumerable<SizeChartMeasurement> measurements)
    {
        if (measurements == null)
        {
            return new List<VariantMeasurementDto>();
        }

        return measurements
            .OrderBy(m => m.DisplayName)
            .ThenBy(m => m.FieldKey)
            .Select(m => new VariantMeasurementDto
            {
                FieldKey = m.FieldKey,
                DisplayName = m.DisplayName,
                Unit = m.Unit,
                MinCm = m.MinCm,
                MaxCm = m.MaxCm,
            })
            .ToList();
    }

    public static string NormalizeFieldKey(string? fieldKey, string? displayName)
    {
        var source = string.IsNullOrWhiteSpace(fieldKey) ? displayName : fieldKey;
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        return new string(source
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray())
            .Trim('-');
    }

    private static List<VariantMeasurementInputDto> Normalize(IEnumerable<VariantMeasurementInputDto>? inputs)
    {
        if (inputs == null)
        {
            return new List<VariantMeasurementInputDto>();
        }

        return inputs
            .Where(input => input != null)
            .Select(input =>
            {
                var normalizedKey = NormalizeFieldKey(input.FieldKey, input.DisplayName);
                var normalizedName = string.IsNullOrWhiteSpace(input.DisplayName)
                    ? input.FieldKey?.Trim() ?? string.Empty
                    : input.DisplayName.Trim();

                return new VariantMeasurementInputDto
                {
                    FieldKey = normalizedKey,
                    DisplayName = normalizedName,
                    Unit = string.IsNullOrWhiteSpace(input.Unit) ? "cm" : input.Unit.Trim(),
                    MinCm = input.MinCm,
                    MaxCm = input.MaxCm,
                };
            })
            .Where(input => !string.IsNullOrWhiteSpace(input.FieldKey))
            .GroupBy(input => input.FieldKey)
            .Select(group => group.First())
            .ToList();
    }
}
