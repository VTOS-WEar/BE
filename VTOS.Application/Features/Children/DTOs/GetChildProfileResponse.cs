using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOS.Application.Features.Children.DTOs;

public record GetChildProfileResponse
{
    public Guid ChildId { get; init; }
    public string FullName { get; init; }
    public int Age { get; init; }
    public string Grade { get; init; }
    public string Gender { get; init; }
    public string SchoolName { get; init; }
    public Guid SchoolId { get; init; }
    public string AvatarUrl { get; init; }
    public ChildBodyMetricDto BodyMetric { get; init; }
    public bool IsStandardSize { get; init; }
}

public record ChildBodyMetricDto(
    int HeightCm,
    float WeightKg
);
