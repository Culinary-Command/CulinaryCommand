using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Inventory.Entities;

namespace CulinaryCommand.Inventory.Services.Interfaces
{
    public interface IUnitService
    {
        // Returns all units asynchronously as a list of Unit objects.
        Task<List<Unit>> GetAllAsync(CancellationToken cancellationToken = default);

        // Retrieves a unit by its Id asynchronously; returns null if not found.
        Task<Unit?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        // Creates a new unit asynchronously and returns the created Unit (including generated values).
        Task<Unit> CreateAsync(Unit unit, CancellationToken cancellationToken = default);

        // Updates the provided unit asynchronously; no value is returned.
        Task UpdateAsync(Unit unit, CancellationToken cancellationToken = default);

        // Deletes the unit with the specified Id asynchronously.
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}