using System.Text.Json;
using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CulinaryCommand.Services
{
    public interface ILocationService
    {
        Task<List<Location>> GetAllLocationsAsync();
        Task<Location?> GetLocationByIdAsync(int id);
        Task<Location?> CreateLocationAsync(Location location, int managerId);
        Task<bool> UpdateLocationAsync(Location location);
        Task<bool> DeleteLocationAsync(int id);

        Task<bool> AddUserToLocationAsync(int locationId, int userId);
        Task<bool> RemoveUserFromLocationAsync(int locationId, int userId);
        Task<List<User>> GetUsersForLocationAsync(int locationId);

        Task<bool> AddManagerToLocationAsync(int locationId, int managerId);
        Task<bool> RemoveManagerFromLocationAsync(int locationId, int managerId);
        Task<List<User>> GetManagersForLocationAsync(int locationId);

        Task<List<Location>> GetLocationsByManagerAsync(int? managerId);
        Task<List<Location>> GetLocationsByCompanyAsync(int? companyId);
        Task<List<Location>> GetLocationsByEmployeeAsync(int? employeeId);


        Task LoadAndPersistLocationsAsync(int userId);
    }

    public class LocationService : ILocationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly LocationState _locationState;
        private readonly IJSRuntime _js;

        public LocationService(IServiceScopeFactory scopeFactory, LocationState locationState, IJSRuntime js)
        {
            _scopeFactory = scopeFactory;
            _locationState = locationState;
            _js = js;
        }

        // helper to get db
        private AppDbContext CreateDb() =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

        // ---------------------- CRUD ----------------------
        public async Task<List<Location>> GetAllLocationsAsync()
        {
            using var db = CreateDb();
            return await db.Locations
                .Include(l => l.Company)
                .Include(l => l.ManagerLocations).ThenInclude(ml => ml.User)
                .ToListAsync();
        }

        public async Task<Location?> GetLocationByIdAsync(int id)
        {
            using var db = CreateDb();
            return await db.Locations
                .Include(l => l.Company)
                .Include(l => l.ManagerLocations).ThenInclude(ml => ml.User)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<Location?> CreateLocationAsync(Location location, int managerId)
        {
            using var db = CreateDb();

            db.Locations.Add(location);
            await db.SaveChangesAsync();

            // Now add relationships
            await AddManagerToLocationAsync(location.Id, managerId);
            await AddUserToLocationAsync(location.Id, managerId);

            return location;
        }

        public async Task<bool> UpdateLocationAsync(Location location)
        {
            using var db = CreateDb();

            var existing = await db.Locations.FindAsync(location.Id);
            if (existing == null) return false;

            existing.Name = location.Name;
            existing.Address = location.Address;
            existing.City = location.City;
            existing.State = location.State;
            existing.ZipCode = location.ZipCode;
            existing.MarginEdgeKey = location.MarginEdgeKey;
            existing.CompanyId = location.CompanyId;

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            using var db = CreateDb();

            var loc = await db.Locations.FindAsync(id);
            if (loc == null) return false;

            db.Locations.Remove(loc);
            await db.SaveChangesAsync();
            return true;
        }

        // ---------------- User Ops ----------------
        public async Task<bool> AddUserToLocationAsync(int locationId, int userId)
        {
            using var db = CreateDb();

            var exists = await db.UserLocations
                .FirstOrDefaultAsync(ul => ul.LocationId == locationId && ul.UserId == userId);

            if (exists != null) return true;

            db.UserLocations.Add(new UserLocation
            {
                LocationId = locationId,
                UserId = userId
            });

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveUserFromLocationAsync(int locationId, int userId)
        {
            using var db = CreateDb();

            var exists = await db.UserLocations
                .FirstOrDefaultAsync(ul => ul.LocationId == locationId && ul.UserId == userId);

            if (exists == null) return false;

            db.UserLocations.Remove(exists);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetUsersForLocationAsync(int locationId)
        {
            using var db = CreateDb();

            return await db.UserLocations
                .Where(ul => ul.LocationId == locationId)
                .Select(ul => ul.User)
                .ToListAsync();
        }

        // ---------------- Manager Ops ----------------
        public async Task<bool> AddManagerToLocationAsync(int locationId, int managerId)
        {
            using var db = CreateDb();

            var location = await db.Locations
                .Include(l => l.ManagerLocations)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            var manager = await db.Users.FindAsync(managerId);

            if (location == null || manager == null) return false;

            bool exists = location.ManagerLocations.Any(ml => ml.UserId == managerId);
            if (!exists)
            {
                db.ManagerLocations.Add(new ManagerLocation
                {
                    UserId = managerId,
                    LocationId = locationId
                });

                await db.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> RemoveManagerFromLocationAsync(int locationId, int managerId)
        {
            using var db = CreateDb();

            var managerLoc = await db.ManagerLocations
                .FirstOrDefaultAsync(ml => ml.LocationId == locationId && ml.UserId == managerId);

            if (managerLoc == null) return false;

            db.ManagerLocations.Remove(managerLoc);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetManagersForLocationAsync(int locationId)
        {
            using var db = CreateDb();

            var location = await db.Locations
                .Include(l => l.ManagerLocations).ThenInclude(ml => ml.User)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            return location?.ManagerLocations.Select(ml => ml.User).ToList()
                ?? new List<User>();
        }

        // ---------------- Queries ----------------
        public async Task<List<Location>> GetLocationsByManagerAsync(int? managerId)
        {
            using var db = CreateDb();

            return await db.Locations
                .Where(l => l.ManagerLocations.Any(ml => ml.UserId == managerId))
                .Include(l => l.ManagerLocations).ThenInclude(ml => ml.User)
                .ToListAsync();
        }

        public async Task<List<Location>> GetLocationsByEmployeeAsync(int? employeeId)
        {
            using var db = CreateDb();

            return await db.Locations
                .Where(l => l.UserLocations.Any(ml => ml.UserId == employeeId))
                .Include(l => l.UserLocations).ThenInclude(ml => ml.User)
                .ToListAsync();
        }

        public async Task<List<Location>> GetLocationsByCompanyAsync(int? companyId)
        {
            using var db = CreateDb();
            return await db.Locations
                .Where(l => l.CompanyId == companyId)
                .ToListAsync();
        }

        // ---------------- Load and Persist ----------------
        public async Task LoadAndPersistLocationsAsync(int userId)
        {
            var locations = await GetLocationsByManagerAsync(userId);
            if (locations == null)
            {
                locations = new List<Location>();
            }

            await _locationState.SetLocationsAsync(locations);

            var json = JsonSerializer.Serialize(locations);
            await _js.InvokeVoidAsync("localStorage.setItem", "cc_locations", json);

            if (_locationState.CurrentLocation != null)
            {
                await _js.InvokeVoidAsync("localStorage.setItem",
                    LocationState.ActiveLocStorageKey,
                    _locationState.CurrentLocation.Id);
            }
        }
    }
}