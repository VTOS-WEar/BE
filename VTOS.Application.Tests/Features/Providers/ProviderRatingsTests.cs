using Microsoft.EntityFrameworkCore;
using VTOS.Application.Features.Providers.Commands;
using VTOS.Application.Features.Providers.Queries;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;
using VTOS.Infrastructure.Persistence;
using Xunit;

namespace VTOS.Application.Tests.Features.Providers;

public class ProviderRatingsTests
{
    [Fact]
    public async Task SubmitProviderRating_CreatesRatingAndUpdatesProviderAggregates()
    {
        await using var db = CreateDbContext();

        var parent = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Parent User",
            Email = "parent@example.com",
            PasswordHash = "hash",
            RoleID = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var school = new School
        {
            Id = Guid.NewGuid(),
            SchoolName = "Test School"
        };

        var child = new ChildProfile
        {
            Id = Guid.NewGuid(),
            ParentUserID = parent.Id,
            ParentUser = parent,
            FullName = "Student A",
            Grade = "1A",
            Gender = Gender.Male,
            SchoolID = school.Id,
            School = school
        };

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            ProviderName = "Alpha Uniform"
        };

        var publication = new SemesterPublication
        {
            Id = Guid.NewGuid(),
            SchoolID = school.Id,
            School = school,
            Semester = "HK1",
            AcademicYear = "2026-2027",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = SemesterPublicationStatus.Active
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ChildProfileID = child.Id,
            ChildProfile = child,
            ProviderID = provider.Id,
            Provider = provider,
            SemesterPublicationID = publication.Id,
            SemesterPublication = publication,
            OrderDate = DateTime.UtcNow.AddDays(-10),
            OrderStatus = OrderStatus.Delivered,
            TotalAmount = 250000,
            ShippingAddress = "123 Street"
        };

        db.Users.Add(parent);
        db.Schools.Add(school);
        db.ChildProfiles.Add(child);
        db.Providers.Add(provider);
        db.SemesterPublications.Add(publication);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new SubmitProviderRatingCommandHandler(db);

        var result = await handler.HandleAsync(new SubmitProviderRatingCommand(
            parent.Id,
            order.Id,
            5,
            "Delivered on time and fit was correct."));

        Assert.True(result.IsSuccess);

        var storedRating = await db.ProviderRatings.SingleAsync();
        Assert.Equal(provider.Id, storedRating.ProviderID);
        Assert.Equal(order.Id, storedRating.OrderID);
        Assert.Equal(parent.Id, storedRating.ParentUserID);
        Assert.Equal(5, storedRating.Rating);

        var storedProvider = await db.Providers.SingleAsync();
        Assert.Equal(5m, storedProvider.AverageRating);
        Assert.Equal(1, storedProvider.TotalRatings);
        Assert.Equal(1, storedProvider.TotalCompletedOrders);
    }

    [Fact]
    public async Task GetProviderRatings_ReturnsNewestRatingsFirst()
    {
        await using var db = CreateDbContext();

        var parent = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Parent User",
            Email = "parent@example.com",
            PasswordHash = "hash",
            RoleID = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            ProviderName = "Alpha Uniform",
            AverageRating = 4.5m,
            TotalRatings = 2,
            TotalCompletedOrders = 3
        };

        var olderOrder = new Order { Id = Guid.NewGuid(), ProviderID = provider.Id, Provider = provider, OrderStatus = OrderStatus.Delivered, ChildProfileID = Guid.NewGuid(), ShippingAddress = "A", OrderDate = DateTime.UtcNow.AddDays(-5), TotalAmount = 100000 };
        var newerOrder = new Order { Id = Guid.NewGuid(), ProviderID = provider.Id, Provider = provider, OrderStatus = OrderStatus.Delivered, ChildProfileID = Guid.NewGuid(), ShippingAddress = "B", OrderDate = DateTime.UtcNow.AddDays(-3), TotalAmount = 120000 };

        db.Users.Add(parent);
        db.Providers.Add(provider);
        db.Orders.AddRange(olderOrder, newerOrder);
        db.ProviderRatings.AddRange(
            new ProviderRating
            {
                Id = Guid.NewGuid(),
                ProviderID = provider.Id,
                Provider = provider,
                OrderID = olderOrder.Id,
                Order = olderOrder,
                ParentUserID = parent.Id,
                ParentUser = parent,
                Rating = 4,
                Comment = "Older",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new ProviderRating
            {
                Id = Guid.NewGuid(),
                ProviderID = provider.Id,
                Provider = provider,
                OrderID = newerOrder.Id,
                Order = newerOrder,
                ParentUserID = parent.Id,
                ParentUser = parent,
                Rating = 5,
                Comment = "Newest",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });

        await db.SaveChangesAsync();

        var handler = new GetProviderRatingsQueryHandler(db);

        var result = await handler.HandleAsync(new GetProviderRatingsQuery(provider.Id));

        Assert.Equal(provider.Id, result.ProviderId);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Newest", result.Items[0].Comment);
        Assert.Equal("Older", result.Items[1].Comment);
        Assert.Equal(4.5m, result.AverageRating);
    }

    private static VTOSDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VTOSDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VTOSDbContext(options);
    }
}
