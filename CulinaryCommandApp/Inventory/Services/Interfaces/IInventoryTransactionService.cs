using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Inventory.Entities;

namespace CulinaryCommand.Inventory.Services.Interfaces
{
    public interface IInventoryTransactionService
    {
        Task<InventoryTransaction> RecordAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);
        Task<List<InventoryTransaction>> GetTransactionsAsync(int? ingredientId = null, CancellationToken cancellationToken = default);
    }
}