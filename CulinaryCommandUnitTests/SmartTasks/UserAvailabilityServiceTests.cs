using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommandApp.SmartTask.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CulinaryCommand.Tests.SmartTask;

public class UserAvailabilityServiceTests
{
    [Fact]
    public async Task GetForUserAsync_ReturnsAvailabilityOrderedByDayOfWeek()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildSeededDbContext(out var dbOptions);
        dbContext.UserAvailabilities.AddRange(
            BuildAvailability(DayOfWeek.Friday, 11, 19),
            BuildAvailability(DayOfWeek.Monday, 8, 16),
            BuildAvailability(DayOfWeek.Wednesday, 9, 17));
        await dbContext.SaveChangesAsync(ct);

        var service = new UserAvailabilityService(BuildDbContextFactory(dbOptions));

        var results = await service.GetForUserAsync(10, 1, ct);

        Assert.Equal(
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
            results.Select(result => result.DayOfWeek).ToArray());
    }

    [Fact]
    public async Task UpsertAsync_AddsNewAvailabilityWhenDayDoesNotExist()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildSeededDbContext(out var dbOptions);
        var service = new UserAvailabilityService(BuildDbContextFactory(dbOptions));

        await service.UpsertAsync(10, 1, DayOfWeek.Tuesday, new TimeOnly(7, 0), new TimeOnly(15, 0), ct);

        await using var verificationContext = new AppDbContext(dbOptions);
        var stored = await verificationContext.UserAvailabilities.SingleAsync(ct);

        Assert.Equal(DayOfWeek.Tuesday, stored.DayOfWeek);
        Assert.Equal(new TimeOnly(7, 0), stored.ShiftStart);
        Assert.Equal(new TimeOnly(15, 0), stored.ShiftEnd);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingAvailabilityWithoutAddingDuplicateRow()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildSeededDbContext(out var dbOptions);
        dbContext.UserAvailabilities.Add(BuildAvailability(DayOfWeek.Monday, 8, 16));
        await dbContext.SaveChangesAsync(ct);

        var service = new UserAvailabilityService(BuildDbContextFactory(dbOptions));

        await service.UpsertAsync(10, 1, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(18, 0), ct);

        await using var verificationContext = new AppDbContext(dbOptions);
        var rows = await verificationContext.UserAvailabilities.ToListAsync(ct);

        Assert.Single(rows);
        Assert.Equal(new TimeOnly(10, 0), rows[0].ShiftStart);
        Assert.Equal(new TimeOnly(18, 0), rows[0].ShiftEnd);
    }

    [Fact]
    public async Task UpsertAsync_ThrowsWhenShiftEndIsNotAfterShiftStart()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildSeededDbContext(out var dbOptions);
        var service = new UserAvailabilityService(BuildDbContextFactory(dbOptions));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertAsync(10, 1, DayOfWeek.Sunday, new TimeOnly(9, 0), new TimeOnly(9, 0), ct));

        await using var verificationContext = new AppDbContext(dbOptions);
        Assert.Empty(await verificationContext.UserAvailabilities.ToListAsync(ct));
    }

    [Fact]
    public async Task DeleteAsync_RemovesMatchingAvailability()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildSeededDbContext(out var dbOptions);
        dbContext.UserAvailabilities.AddRange(
            BuildAvailability(DayOfWeek.Monday, 8, 16),
            BuildAvailability(DayOfWeek.Tuesday, 9, 17));
        await dbContext.SaveChangesAsync(ct);

        var service = new UserAvailabilityService(BuildDbContextFactory(dbOptions));

        await service.DeleteAsync(10, 1, DayOfWeek.Monday, ct);

        await using var verificationContext = new AppDbContext(dbOptions);
        var remaining = await verificationContext.UserAvailabilities.ToListAsync(ct);

        Assert.Single(remaining);
        Assert.Equal(DayOfWeek.Tuesday, remaining[0].DayOfWeek);
    }

    private static IDbContextFactory<AppDbContext> BuildDbContextFactory(DbContextOptions<AppDbContext> dbOptions)
    {
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(dbOptions));
        return factory.Object;
    }

    private static AppDbContext BuildSeededDbContext(out DbContextOptions<AppDbContext> dbOptions)
    {
        dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(dbOptions);
        dbContext.Locations.Add(new Location
        {
            Id = 1,
            Name = "Main Kitchen",
            CompanyId = 1,
            Address = "123 Test St",
            City = "Testville",
            State = "IA",
            ZipCode = "50000"
        });
        dbContext.Users.Add(new User
        {
            Id = 10,
            Name = "Taylor Cook",
            Email = "taylor@example.com",
            Role = "Employee",
            IsActive = true
        });
        dbContext.SaveChanges();
        return dbContext;
    }

    private static UserAvailability BuildAvailability(DayOfWeek dayOfWeek, int startHour, int endHour) =>
        new()
        {
            UserId = 10,
            LocationId = 1,
            DayOfWeek = dayOfWeek,
            ShiftStart = new TimeOnly(startHour, 0),
            ShiftEnd = new TimeOnly(endHour, 0)
        };
}
