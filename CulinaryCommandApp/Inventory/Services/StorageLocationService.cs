using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CulinaryCommandApp.Inventory.Entities;
using CulinaryCommand.Data;

namespace CulinaryCommandApp.Inventory.Services
{
    public class StorageLocationService
    {
        private readonly AppDbContext _db;

        public StorageLocationService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<StorageLocation>> GetByLocationAsync(int locationId)
        {
            return await _db.StorageLocations
                .Where(sl => sl.LocationId == locationId)
                .ToListAsync();
        }

        public async Task<StorageLocation?> GetByIdAsync(int id)
        {
            return await _db.StorageLocations.FindAsync(id);
        }

        public async Task<StorageLocation> AddAsync(StorageLocation storageLocation)
        {
            _db.StorageLocations.Add(storageLocation);
            await _db.SaveChangesAsync();
            return storageLocation;
        }

        public async Task<bool> UpdateAsync(StorageLocation storageLocation)
        {
            _db.StorageLocations.Update(storageLocation);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.StorageLocations.FindAsync(id);
            if (entity == null) return false;
            _db.StorageLocations.Remove(entity);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
