using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Children.DTOs;

namespace VTOS.Application.Features.Children.Queries
{
    public class GetChildProfileQueryHandler : IGetChildProfileQueryHandler
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetChildProfileQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<GetChildProfileResponse>> HandleAsync(GetChildProfileQuery query, CancellationToken cancellationToken = default)
        {
             // Find child by id
        var child = await _context.ChildProfiles.Include(x=>x.School).FirstOrDefaultAsync(u => u.Id == query.ChildId, cancellationToken);

        if (child == null)
        {
            return Result<GetChildProfileResponse>.Failure(
                "Child not found",
                "CHILD_NOT_FOUND"
            );
        }

        // Check if child is deleted
        if (child.IsDeleted)
        {
            return Result<GetChildProfileResponse>.Failure(
                "Account is disabled",
                "ACCOUNT_DISABLED");
        }
            GetChildProfileResponse childResponse = _mapper.Map<GetChildProfileResponse>(child);
            return Result<GetChildProfileResponse>.Success(childResponse);
        }
    }
}
