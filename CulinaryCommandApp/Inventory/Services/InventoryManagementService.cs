using System.Linq;
using CulinaryCommandApp.Inventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using CulinaryCommandApp.Inventory.DTOs;
using CulinaryCommand.Data;
using CulinaryCommandApp.Inventory.Entities;

namespace CulinaryCommandApp.Inventory.Services
{
    public class InventoryManagementService : IInventoryManagementService
    {
        private readonly AppDbContext _db;

        public InventoryManagementService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<InventoryItemDTO>> GetAllItemsAsync()
        {
            return await _db.Ingredients
                .Include(i => i.Unit)
                .Include(i => i.StorageLocation)
                .Select(ingredient => new InventoryItemDTO
                {
                    Id = ingredient.Id,
                    Name = ingredient.Name,
                    Category = ingredient.Category,
                    CurrentQuantity = ingredient.StockQuantity,
                    Unit = ingredient.Unit != null ? ingredient.Unit.Name : "count",
                    SKU = ingredient.Sku ?? "",
                    Price = ingredient.Price ?? 0m,
                    ReorderLevel = ingredient.ReorderLevel,
                    IsLowStock = ingredient.StockQuantity <= ingredient.ReorderLevel && ingredient.StockQuantity > 0,
                    OutOfStockDate = null,
                    LastOrderDate = null,
                    Notes = ingredient.Notes ?? "",
                    StorageLocationId = ingredient.StorageLocationId,
                    StorageLocationName = ingredient.StorageLocation != null ? ingredient.StorageLocation.Name : null
                })
                .ToListAsync();
        }

        public async Task<List<InventoryItemDTO>> GetItemsByLocationAsync(int locationId)
        {
            return await _db.Ingredients
                .Include(i => i.Unit)
                .Include(i => i.Vendor)
                .Include(i => i.StorageLocation)
                .Where(i => i.LocationId == locationId)
                .Select(ingredient => new InventoryItemDTO
                {
                    Id = ingredient.Id,
                    Name = ingredient.Name,
                    Category = ingredient.Category,
                    CurrentQuantity = ingredient.StockQuantity,
                    Unit = ingredient.Unit != null ? ingredient.Unit.Name : "count",
                    SKU = ingredient.Sku ?? "",
                    Price = ingredient.Price ?? 0m,
                    ReorderLevel = ingredient.ReorderLevel,
                    IsLowStock = ingredient.StockQuantity <= ingredient.ReorderLevel && ingredient.StockQuantity > 0,
                    OutOfStockDate = null,
                    LastOrderDate = null,
                    Notes = ingredient.Notes ?? "",
                    VendorId = ingredient.VendorId,
                    VendorName = ingredient.Vendor != null ? ingredient.Vendor.Name : null,
                    VendorLogoUrl = ingredient.Vendor != null ? ingredient.Vendor.LogoUrl : null,
                    StorageLocationId = ingredient.StorageLocationId,
                    StorageLocationName = ingredient.StorageLocation != null ? ingredient.StorageLocation.Name : null
                })
                .ToListAsync();
        }

        public async Task<List<string>> GetCategoriesByLocationAsync(int locationId)
        {
            return await _db.Ingredients
                .Where(i => i.LocationId == locationId && !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<InventoryItemDTO> AddItemAsync(CreateIngredientDTO dto)
        {
            var entity = new CulinaryCommandApp.Inventory.Entities.Ingredient
            {
                Name = dto.Name,
                Sku = dto.SKU,
                Price = dto.Price,
                Category = dto.Category ?? string.Empty,
                StockQuantity = dto.CurrentQuantity,
                ReorderLevel = dto.ReorderLevel,
                UnitId = dto.UnitId,
                LocationId = dto.LocationId,
                VendorId = dto.VendorId,
                StorageLocationId = dto.StorageLocationId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Ingredients.Add(entity);
            await _db.SaveChangesAsync();
            return new InventoryItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                SKU = entity.Sku ?? string.Empty,
                Category = entity.Category ?? string.Empty,
                CurrentQuantity = entity.StockQuantity,
                Unit = entity.Unit != null ? entity.Unit.Name : "count",
                Price = entity.Price ?? 0m,
                ReorderLevel = entity.ReorderLevel,
                IsLowStock = entity.StockQuantity <= entity.ReorderLevel,
                OutOfStockDate = null,
                LastOrderDate = null,
                Notes = entity.Notes ?? string.Empty,
                StorageLocationId = entity.StorageLocationId,
                StorageLocationName = entity.StorageLocation != null ? entity.StorageLocation.Name : null
            };
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var entity = await _db.Ingredients.FindAsync(id);
            if (entity == null)
                return false;

            _db.Ingredients.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<InventoryItemDTO?> UpdateItemAsync(InventoryItemDTO dto)
        {
            var entity = await _db.Ingredients.FindAsync(dto.Id);
            if (entity == null)
                return null;

            // Map mutable fields from DTO back to the entity
            entity.Name = dto.Name;
            entity.Sku = string.IsNullOrWhiteSpace(dto.SKU) ? null : dto.SKU;
            entity.StockQuantity = dto.CurrentQuantity;
            entity.Price = dto.Price;
            entity.ReorderLevel = dto.ReorderLevel;
            entity.Category = dto.Category;
            entity.UnitId = dto.UnitId;
            entity.VendorId = dto.VendorId;
            entity.StorageLocationId = dto.StorageLocationId;

            await _db.SaveChangesAsync();

            return new InventoryItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                SKU = entity.Sku ?? string.Empty,
                Category = entity.Category ?? string.Empty,
                CurrentQuantity = entity.StockQuantity,
                Unit = entity.Unit != null ? entity.Unit.Name : "count",
                UnitId = entity.UnitId,
                Price = entity.Price ?? 0m,
                ReorderLevel = entity.ReorderLevel,
                IsLowStock = entity.StockQuantity <= entity.ReorderLevel,
                OutOfStockDate = null,
                LastOrderDate = null,
                Notes = entity.Notes ?? string.Empty,
                VendorId = entity.VendorId,
                StorageLocationId = entity.StorageLocationId,
                StorageLocationName = entity.StorageLocation != null ? entity.StorageLocation.Name : null
            };
        }
    }
}