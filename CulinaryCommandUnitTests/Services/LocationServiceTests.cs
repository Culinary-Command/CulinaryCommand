using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommand.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace CulinaryCommand.Tests.Services;

public class LocationServiceTests
{
    [Fact]
    public async Task LoadAndPersistLocationsAsync_UsesEmployeeAssignmentsForEmployeeUsers()
    {
        var databaseName = Guid.NewGuid().ToString();
        var provider = BuildServiceProvider(databaseName);

        await SeedEmployeeAsync(provider);

        var jsRuntime = new FakeJsRuntime();
        var locationState = new LocationState(jsRuntime);
        var service = new LocationService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            locationState,
            jsRuntime);

        await service.LoadAndPersistLocationsAsync(101);

        Assert.Single(locationState.ManagedLocations);
        Assert.Equal(11, locationState.ManagedLocations[0].Id);
        Assert.Equal(11, locationState.CurrentLocation?.Id);

        var locationsJson = Assert.IsType<string>(jsRuntime.Storage["cc_locations"]);
        var persistedLocations = JsonSerializer.Deserialize<List<Location>>(locationsJson);

        Assert.NotNull(persistedLocations);
        Assert.Single(persistedLocations!);
        Assert.Equal(11, persistedLocations[0].Id);
        Assert.Equal(11, Assert.IsType<int>(jsRuntime.Storage[LocationState.ActiveLocStorageKey]));
    }

    private static ServiceProvider BuildServiceProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        return services.BuildServiceProvider();
    }

    private static async Task SeedEmployeeAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var location = new Location
        {
            Id = 11,
            Name = "Prep Kitchen",
            CompanyId = 1,
            Address = "123 Test St",
            City = "Ames",
            State = "IA",
            ZipCode = "50010"
        };

        var employee = new User
        {
            Id = 101,
            Name = "Employee Example",
            Email = "employee@example.com",
            Role = "Employee",
            IsActive = true,
            CompanyId = 1
        };

        db.Locations.Add(location);
        db.Users.Add(employee);
        db.UserLocations.Add(new UserLocation
        {
            UserId = employee.Id,
            LocationId = location.Id
        });

        await db.SaveChangesAsync();
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public Dictionary<string, object?> Storage { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "localStorage.getItem")
            {
                var key = args?.FirstOrDefault()?.ToString() ?? string.Empty;
                Storage.TryGetValue(key, out var value);
                if (value is null)
                {
                    return new ValueTask<TValue>(default(TValue)!);
                }

                return new ValueTask<TValue>((TValue)value);
            }

            if (identifier == "localStorage.setItem")
            {
                var key = args?.ElementAtOrDefault(0)?.ToString() ?? string.Empty;
                Storage[key] = args?.ElementAtOrDefault(1);
                return new ValueTask<TValue>(default(TValue)!);
            }

            if (identifier == "localStorage.removeItem")
            {
                var key = args?.FirstOrDefault()?.ToString() ?? string.Empty;
                Storage.Remove(key);
                return new ValueTask<TValue>(default(TValue)!);
            }

            throw new NotSupportedException($"Unexpected JS interop call: {identifier}");
        }
    }
}
