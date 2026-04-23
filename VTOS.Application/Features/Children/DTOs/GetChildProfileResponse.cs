using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOS.Application.Features.Children.DTOs;

public record GetChildProfileResponse
{
    public Guid ChildId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public int Age { get; init; }
    public string Grade { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public string SchoolName { get; init; } = string.Empty;
    public Guid SchoolId { get; init; }
    public string AvatarUrl { get; init; } = string.Empty;
    public ChildBodyMetricDto BodyMetric { get; init; } = new(0, 0);
    public bool IsStandardSize { get; init; }
}

public record ChildBodyMetricDto(
    int HeightCm,
    float WeightKg
);
