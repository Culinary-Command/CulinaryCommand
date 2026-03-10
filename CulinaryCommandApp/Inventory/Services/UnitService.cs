using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommand.Inventory.Entities;
using CulinaryCommand.Inventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CulinaryCommand.Inventory.Services
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
    }
}
