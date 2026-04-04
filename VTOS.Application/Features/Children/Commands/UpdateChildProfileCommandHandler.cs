using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Children.DTOs;

namespace VTOS.Application.Features.Children.Commands
{
    public class UpdateChildProfileCommandHandler : IUpdateChildProfileCommandHandler
    {
        private readonly IApplicationDbContext _context;
        public UpdateChildProfileCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Result<UpdateChildProfileResponse>> HandleAsync(UpdateChildProfileCommand command, CancellationToken cancellationToken = default)
        {
            var child = await _context.ChildProfiles
           .Include(x => x.School)
           .FirstOrDefaultAsync(x => x.Id == command.ChildId, cancellationToken);

            if (child == null)
                return Result<UpdateChildProfileResponse>.Failure("Child not found", "CHILD_NOT_FOUND");

            bool isUpdated = false;

            // Update FullName
            if (!string.IsNullOrWhiteSpace(command.FullName)
                && command.FullName != child.FullName)
            {
                child.FullName = command.FullName;
                isUpdated = true;
            }

            // Update DOB and recalculate Age
            if (command.DOB.HasValue && command.DOB != child.DOB)
            {
                child.DOB = command.DOB;
                child.Age = CalculateAge(command.DOB.Value);
                isUpdated = true;
            }

            // Update Grade
            if (!string.IsNullOrWhiteSpace(command.Grade)
                && command.Grade != child.Grade)
            {
                child.Grade = command.Grade;
                isUpdated = true;
            }

            // Update Gender
            if (command.Gender.HasValue && command.Gender != child.Gender)
            {
                child.Gender = command.Gender.Value;
                isUpdated = true;
            }

            // Update HeightCm
            if (command.HeightCm.HasValue && command.HeightCm != child.HeightCm)
            {
                child.HeightCm = command.HeightCm.Value;
                isUpdated = true;
            }

            // Update WeightKg
            if (command.WeightKg.HasValue && command.WeightKg != child.WeightKg)
            {
                child.WeightKg = command.WeightKg.Value;
                isUpdated = true;
            }

            if (isUpdated)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result<UpdateChildProfileResponse>.Success(
                new UpdateChildProfileResponse(
                    child.Id,
                    child.FullName,
                    child.Age,
                    child.Grade,
                    child.Gender.ToString(),
                    child.School.SchoolName,
                    child.SchoolID,
                    child.Avatar,
                    new ChildBodyMetricDto(child.HeightCm, child.WeightKg),
                    IsPhysicallyPossible(child.HeightCm, child.WeightKg)
                )
            );

        }
        private static int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age))
                age--;
            return age;
        }

        public static bool IsPhysicallyPossible(int heightCm, float weightKg)
        {
            return heightCm >= 50 && heightCm <= 200
                && weightKg >= 5 && weightKg <= 120;
        }
    }
}
