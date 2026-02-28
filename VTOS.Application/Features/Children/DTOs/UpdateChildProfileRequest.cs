using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Children.DTOs;

public record UpdateChildProfileRequest(
    string ChildId,
    string? FullName,
    DateTime? DOB,
    string? Grade,
    Gender? Gender,
    int? HeightCm,
    float? WeightKg
);
