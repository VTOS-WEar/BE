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
    public class GetMyChildProfileQueryHandler : IGetMyChildProfileQueryHandler
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetMyChildProfileQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<ICollection<GetChildProfileResponse>>> HandleAsync(GetMyChildProfileQuery query, CancellationToken cancellationToken = default)
        {
             // Find user by id
        var user = await _context.Users.Include(x=>x.ChildProfiles).ThenInclude(x=>x.School).FirstOrDefaultAsync(u => u.Id == query.Id, cancellationToken);

        if (user == null)
        {
            return Result<ICollection<GetChildProfileResponse>>.Failure(
                "User not found",
                "USER_NOT_FOUND"
            );
        }

        // Check if user is deleted
        if (user.IsDeleted)
        {
            return Result<ICollection<GetChildProfileResponse>>.Failure(
                "Account is disabled",
                "ACCOUNT_DISABLED");
        }
            ICollection<GetChildProfileResponse> list = _mapper.Map<ICollection<GetChildProfileResponse>>(user.ChildProfiles);
        return Result<ICollection<GetChildProfileResponse>>.Success(list);
        }
    }
}
