using Microsoft.EntityFrameworkCore;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(
        VTOSDbContext context,
        IPasswordHasher passwordHasher)
    {
        await context.Database.MigrateAsync();

        // ========================
        // ROLES
        // ========================
        var adminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == "Admin");

        if (adminRole == null)
        {
            adminRole = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = "Admin"
            };

            context.Roles.Add(adminRole);
            await context.SaveChangesAsync();
        }

        var parentRole = await context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == "Parent");

        if (parentRole == null)
        {
            parentRole = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = "Parent"
            };

            context.Roles.Add(parentRole);
            await context.SaveChangesAsync();
        }

        // ========================
        // ADMIN USER
        // ========================
        if (!await context.Users.AnyAsync(u => u.Email == "admin@vtos.com"))
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@vtos.com",
                FullName = "Quản trị viên",
                PasswordHash = passwordHasher.HashPassword("Test@1234"),
                RoleID = adminRole.Id,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        // ========================
        // USERS (only test users)
        // ========================
        if (!await context.Users.AnyAsync(u => u.Email == "pending@vtos.com"))
        {
            var pendingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "pending@vtos.com",
                FullName = "Pending User",
                PasswordHash = passwordHasher.HashPassword("123456"),
                RoleID = parentRole.Id,
                IsActive = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(pendingUser);
        }

        if (!await context.Users.AnyAsync(u => u.Email == "active@vtos.com"))
        {
            var activeUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "active@vtos.com",
                FullName = "Active User",
                PasswordHash = passwordHasher.HashPassword("123456"),
                RoleID = parentRole.Id,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(activeUser);
        }

        await context.SaveChangesAsync();

        // ========================
        // SCHOOL
        // ========================
        var school = await context.Schools.FirstOrDefaultAsync();

        if (school == null)
        {
            school = new School
            {
                Id = Guid.NewGuid(),
                SchoolName = "Demo School",
                CreatedAt = DateTime.UtcNow
            };

            context.Schools.Add(school);
            await context.SaveChangesAsync();
        }

        // ===== Size Chart =====
        var sizeChart = await context.SizeCharts.FirstOrDefaultAsync();

        if (sizeChart == null)
        {
            sizeChart = new SizeChart
            {
                ChartName = "Default Size Chart",
                Description = "Standard student uniform size chart",
                Unit = "cm"
            };

            context.SizeCharts.Add(sizeChart);
            await context.SaveChangesAsync();
        }


        // ========================
        // OUTFIT
        // ========================
        var outfit = await context.Outfits.FirstOrDefaultAsync();

        if (outfit == null)
        {
            outfit = new Outfit
            {
                Id = Guid.NewGuid(),
                OutfitName = "Sample Uniform",
                Description = "Demo outfit",
                Price = 100,
                SchoolID = school.Id,
                SizeChartID = sizeChart.Id,
                CreatedAt = DateTime.UtcNow,
                IsAvailable = true
            };

            context.Outfits.Add(outfit);
            await context.SaveChangesAsync();
        }

        // ========================
        // FEEDBACK
        // ========================
        if (!await context.Feedbacks.AnyAsync())
        {
            var activeUser = await context.Users
                .FirstAsync(u => u.Email == "active@vtos.com");

            var pendingUser = await context.Users
                .FirstAsync(u => u.Email == "pending@vtos.com");

            var feedbacks = new List<Feedback>
            {
                new Feedback
                {
                    Id = Guid.NewGuid(),
                    UserID = activeUser.Id,
                    OutfitID = outfit.Id,
                    Rating = 5,
                    Comment = "Very good system",
                    Timestamp = DateTime.UtcNow,
                    ModerationStatus = ModerationStatus.Approved
                },
                new Feedback
                {
                    Id = Guid.NewGuid(),
                    UserID = pendingUser.Id,
                    OutfitID = outfit.Id,
                    Rating = 1,
                    Comment = "Spam content remove me",
                    Timestamp = DateTime.UtcNow,
                    ModerationStatus = ModerationStatus.Pending
                }
            };

            context.Feedbacks.AddRange(feedbacks);
            await context.SaveChangesAsync();
        }
    }
}
