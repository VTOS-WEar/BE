using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOS.Application.Features.Children.DTOs;
public record UpdateChildProfileResponse(
    Guid ChildId,
    string FullName,
    int Age,
    string Grade,
    string Gender,
    string SchoolName,
    Guid SchoolId,
    string AvatarUrl,
    ChildBodyMetricDto BodyMetric,
    bool IsStandardSize
);

