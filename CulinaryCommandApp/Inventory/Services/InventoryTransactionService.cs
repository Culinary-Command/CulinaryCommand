using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CulinaryCommand.Data;
using CulinaryCommandApp.Inventory.Entities;
using CulinaryCommandApp.Inventory.Services.Interfaces;



namespace CulinaryCommandApp.Inventory.Services
{
    public class InventoryTransactionService : IInventoryTransactionService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<InventoryTransactionService> _logger;

        public InventoryTransactionService(AppDbContext db, ILogger<InventoryTransactionService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<InventoryTransaction> RecordAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (transaction.StockChange == 0)
                throw new ArgumentException("StockChange must be non-zero.", nameof(transaction));

            if (transaction.IngredientId <= 0)
                throw new ArgumentException("IngredientId must be a positive integer.", nameof(transaction));

            var transactionAmount = transaction.StockChange; // if positive then add, if negative then remove

            _logger.LogDebug("Recording inventory transaction for ingredient {IngredientId} amount {Amount}", transaction.IngredientId, transactionAmount);

            // using an explicit db transaction so update and insert are atomic.
            await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Ingredients SET StockQuantity = StockQuantity + {transactionAmount} WHERE IngredientId = {transaction.IngredientId} AND StockQuantity + {transactionAmount} >= 0",
                cancellationToken);

            if (rows == 0)
            {
                _logger.LogWarning("Conditional stock update did not affect any rows for ingredient {IngredientId} with delta {Delta}", transaction.IngredientId, transactionAmount);
                throw new InvalidOperationException("Insufficient stock or ingredient not found.");
            }


            // insert audit/transaction row
            transaction.CreatedAt = DateTimeOffset.UtcNow;
            await _db.InventoryTransactions.AddAsync(transaction, cancellationToken);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Recorded inventory transaction {TransactionId} for ingredient {IngredientId}", transaction.Id, transaction.IngredientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record inventory transaction for ingredient {IngredientId}", transaction.IngredientId);
                try
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rbEx)
                {
                    _logger.LogError(rbEx, "Rollback failed after transaction error for ingredient {IngredientId}", transaction.IngredientId);
                }

                throw;
            }

            return transaction;
        }

        public async Task<List<InventoryTransaction>> RecordBatchAsync(
            IEnumerable<InventoryTransaction> transactions,
            CancellationToken cancellationToken = default)
        {
            if (transactions is null)
                throw new ArgumentNullException(nameof(transactions));

            var batch = transactions.ToList();
            if (batch.Count == 0)
                return new List<InventoryTransaction>();

            await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var transaction in batch)
                {
                    if (transaction.StockChange == 0)
                        throw new ArgumentException($"StockChange must be non-zero (IngredientId {transaction.IngredientId}).");
                    if (transaction.IngredientId <= 0)
                        throw new ArgumentException($"IngredientId must be a positive integer (value: {transaction.IngredientId}).");

                    var delta = transaction.StockChange;

                    _logger.LogDebug(
                        "Batch: recording transaction for ingredient {IngredientId} delta {Delta}",
                        transaction.IngredientId, delta);

                    var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE Ingredients SET StockQuantity = StockQuantity + {delta} WHERE IngredientId = {transaction.IngredientId} AND StockQuantity + {delta} >= 0",
                        cancellationToken);

                    if (rows == 0)
                    {
                        _logger.LogWarning(
                            "Batch stock update failed for ingredient {IngredientId} with delta {Delta} — rolling back entire batch",
                            transaction.IngredientId, delta);
                        throw new InvalidOperationException(
                            $"Insufficient stock or ingredient not found (IngredientId {transaction.IngredientId}). No inventory was changed.");
                    }

                    transaction.CreatedAt = DateTimeOffset.UtcNow;
                    await _db.InventoryTransactions.AddAsync(transaction, cancellationToken);
                }

                await _db.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Committed batch of {Count} inventory transactions", batch.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch transaction failed — rolling back");
                try
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rbEx)
                {
                    _logger.LogError(rbEx, "Rollback failed after batch error");
                }

                throw;
            }

            return batch;
        }

        public async Task<List<InventoryTransaction>> GetTransactionsAsync(int? ingredientId = null, CancellationToken cancellationToken = default)
        {
            var query = _db.InventoryTransactions
                        .AsNoTracking()
                        .AsQueryable();

            if (ingredientId.HasValue)
                query = query.Where(t => t.IngredientId == ingredientId.Value);

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }


    }
}