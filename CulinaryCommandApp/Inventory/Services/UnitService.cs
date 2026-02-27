using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommandApp.Inventory.Entities;
using CulinaryCommandApp.Inventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CulinaryCommandApp.Inventory.Services
{
    public class UnitService : IUnitService
    {
        private readonly AppDbContext _db;

        public UnitService(AppDbContext db)
        {
            _db = db;
        }

        // Interface implementation ------------------------------------------------
        public async Task<List<Unit>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Units
                .OrderBy(unit => unit.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Unit?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.Units.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<Unit> CreateAsync(Unit unit, CancellationToken cancellationToken = default)
        {
            await _db.Units.AddAsync(unit, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return unit;
        }

        public async Task UpdateAsync(Unit unit, CancellationToken cancellationToken = default)
        {
            _db.Units.Update(unit);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Units.FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                _db.Units.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        // Backwards-compatible helpers --------------------------------------------
        /// Return all inventory units used by the inventory subsystem.
        public async Task<List<Unit>> GetAllUnitsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAllAsync(cancellationToken);
        }

        // For compatibility: return units that could be used for the given ingredient.
        // Currently returns all units (no per-ingredient restriction).
        public async Task<List<Unit>> GetUnitsForIngredient(int ingredientId, CancellationToken cancellationToken = default)
        {
            return await GetAllAsync(cancellationToken);
        }


        public async Task<List<Unit>> GetByLocationAsync(int locationId, CancellationToken cancellationToken = default)
        {
            return await _db.LocationUnits
                        .Where(lu => lu.LocationId == locationId)
                        .Include(lu => lu.Unit)
                        .Select(lu => lu.Unit)
                        .OrderBy(u => u.Name)
                        .ToListAsync(cancellationToken);
        }


        public async Task SetLocationUnitsAsync(int locationId, IEnumerable<int> unitIds, CancellationToken cancellationToken = default)
        {
            var desired = unitIds.ToHashSet();

            var existing = await _db.LocationUnits
                                .Where(lu => lu.LocationId == locationId)
                                .ToListAsync(cancellationToken);

            var existingIds = existing.Select(lu => lu.UnitId).ToHashSet();

            // Remove units no longer in the list
            var toRemove = existing.Where(lu => !desired.Contains(lu.UnitId)).ToList();
            _db.LocationUnits.RemoveRange(toRemove);

            // Add new units
            foreach (var unitId in desired.Where(id => !existingIds.Contains(id)))
            {
                _db.LocationUnits.Add(new LocationUnit
                {
                    LocationId = locationId,
                    UnitId = unitId,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
