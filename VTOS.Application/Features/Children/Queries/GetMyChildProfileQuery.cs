using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTOS.Application.Common;
using VTOS.Application.Features.Children.DTOs;

namespace VTOS.Application.Features.Children.Queries
{
    public record GetMyChildProfileQuery(
       Guid Id
    );
    public interface IGetMyChildProfileQueryHandler
    {
        Task<Result<ICollection<GetChildProfileResponse>>> HandleAsync(GetMyChildProfileQuery query, CancellationToken cancellationToken = default);
    }
}
