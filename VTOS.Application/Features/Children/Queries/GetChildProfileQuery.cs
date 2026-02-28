using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTOS.Application.Common;
using VTOS.Application.Features.Children.DTOs;

namespace VTOS.Application.Features.Children.Queries
{
    public record GetChildProfileQuery(
       Guid ChildId
    );
    public interface IGetChildProfileQueryHandler
    {
        Task<Result<GetChildProfileResponse>> HandleAsync(GetChildProfileQuery query, CancellationToken cancellationToken = default);
    }
}
