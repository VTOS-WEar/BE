using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;
using VTOS.Application.Features.Users.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Users.Commands
{
    public class UpdateProfileCommandHandler: IUpdateProfileCommandHandler
    {
        private readonly IApplicationDbContext _context;
        public UpdateProfileCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<UpdateProfileResponse>> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.Include(x=>x.Role).FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

            if (user == null)
                return Result<UpdateProfileResponse>.Failure("User not found","USER_NOT_FOUND");
            bool isUpdated = false;

            if (!string.IsNullOrWhiteSpace(command.FullName)
                && command.FullName != user.FullName)
            {
                user.FullName = command.FullName;
                isUpdated = true;
            }

            if (command.DOB.HasValue && command.DOB != user.DOB)
            {
                user.DOB = command.DOB;
                isUpdated = true;
            }

            if (command.Gender.HasValue && command.Gender != user.Gender)
            {
                user.Gender = command.Gender.Value;
                isUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Phone)
                && command.Phone != user.Phone)
            {
                user.Phone = command.Phone;
                isUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Email)
                && command.Email != user.Email)
            {
                user.Email = command.Email;
                isUpdated = true;
            }
            if (!isUpdated)
                return Result<UpdateProfileResponse>.Success(new UpdateProfileResponse(
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Phone ?? string.Empty,
                    user.DOB ?? DateTime.Now.AddYears(-18),
                    user.Gender.ToString(),
                    user.Role.RoleName,
                    user.IsActive,
                    user.IsDeleted,
                    user.CreatedAt,
                    user.LastLogin ?? DateTime.MinValue
                    )
                );
            await _context.SaveChangesAsync(cancellationToken);
            return Result<UpdateProfileResponse>.Success(new UpdateProfileResponse(
                   user.Id,
                   user.Email,
                   user.FullName,
                   user.Phone ?? string.Empty,
                   user.DOB ?? DateTime.Now.AddYears(-18),
                   user.Gender.ToString(),
                   user.Role.RoleName,
                   user.IsActive,
                   user.IsDeleted,
                   user.CreatedAt,
                   user.LastLogin ?? DateTime.MinValue
                   )
               );
        }
    }
}
