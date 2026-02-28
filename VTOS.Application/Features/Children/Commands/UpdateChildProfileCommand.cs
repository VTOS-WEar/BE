using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTOS.Application.Common;
using VTOS.Application.Features.Children.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Children.Commands;
public record UpdateChildProfileCommand(
    Guid ChildId,
    string? FullName,
    DateTime? DOB,
    string? Grade,
    Gender? Gender,
    int? HeightCm,
    float? WeightKg
);
public interface IUpdateChildProfileCommandHandler
{
    Task<Result<UpdateChildProfileResponse>> HandleAsync(UpdateChildProfileCommand command, CancellationToken cancellationToken = default);
}