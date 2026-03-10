using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommandApp.Inventory.Entities;

namespace CulinaryCommandApp.Inventory.Services.Interfaces
{
    public interface IInventoryTransactionService
    {
        Task<InventoryTransaction> RecordAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deducts stock for every transaction in <paramref name="transactions"/> atomically.
        /// All stock updates and audit rows are committed in a single database transaction;
        /// if any deduction fails (e.g. insufficient stock) the entire batch is rolled back
        /// and an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        Task<List<InventoryTransaction>> RecordBatchAsync(IEnumerable<InventoryTransaction> transactions, CancellationToken cancellationToken = default);

        Task<List<InventoryTransaction>> GetTransactionsAsync(int? ingredientId = null, CancellationToken cancellationToken = default);
    }
}