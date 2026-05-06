using System;
using System.Linq;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommandApp.Inventory.DTOs;
using CulinaryCommandApp.Inventory.Entities;
using CulinaryCommandApp.Inventory.Services;
using Microsoft.EntityFrameworkCore;
using VendorEntity = CulinaryCommand.Vendor.Entities.Vendor;

namespace CulinaryCommand.Tests.Inventory.Services;

public class InventoryManagementServiceTests
{
    [Fact]
    public async Task GetItemsByLocationAsync_ReturnsMappedItemsForRequestedLocation()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedReferenceData(dbContext);

        dbContext.Ingredients.AddRange(
            new Ingredient
            {
                Id = 1,
                Name = "Milk",
                LocationId = 1,
                UnitId = 1,
                VendorId = 1,
                StorageLocationId = 1,
                Category = "Dairy",
                StockQuantity = 2m,
                ReorderLevel = 3m,
                Price = 4.25m,
                Sku = "MILK-1",
                Notes = "Whole milk"
            },
            new Ingredient
            {
                Id = 2,
                Name = "Flour",
                LocationId = 1,
                UnitId = 2,
                Category = "Dry Goods",
                StockQuantity = 15m,
                ReorderLevel = 5m
            },
            new Ingredient
            {
                Id = 3,
                Name = "Tomatoes",
                LocationId = 2,
                UnitId = 1,
                Category = "Produce",
                StockQuantity = 6m,
                ReorderLevel = 2m
            });
        await dbContext.SaveChangesAsync(ct);

        var service = new InventoryManagementService(dbContext);

        var items = await service.GetItemsByLocationAsync(1);

        Assert.Equal(2, items.Count);
        var milk = items.Single(item => item.Id == 1);
        Assert.Equal("Acme Supply", milk.VendorName);
        Assert.Equal("https://example.com/logo.png", milk.VendorLogoUrl);
        Assert.Equal("Walk-in", milk.StorageLocationName);
        Assert.Equal("Liter", milk.Unit);
        Assert.True(milk.IsLowStock);
        Assert.DoesNotContain(items, item => item.Id == 3);
    }

    [Fact]
    public async Task GetCategoriesByLocationAsync_ReturnsDistinctSortedNonEmptyCategories()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedReferenceData(dbContext);

        dbContext.Ingredients.AddRange(
            new Ingredient { Id = 1, Name = "Milk", LocationId = 1, UnitId = 1, Category = "Dairy" },
            new Ingredient { Id = 2, Name = "Cheese", LocationId = 1, UnitId = 1, Category = "Dairy" },
            new Ingredient { Id = 3, Name = "Flour", LocationId = 1, UnitId = 2, Category = "Baking" },
            new Ingredient { Id = 4, Name = "Blank", LocationId = 1, UnitId = 2, Category = string.Empty },
            new Ingredient { Id = 5, Name = "Offsite", LocationId = 2, UnitId = 2, Category = "Produce" });
        await dbContext.SaveChangesAsync(ct);

        var service = new InventoryManagementService(dbContext);

        var categories = await service.GetCategoriesByLocationAsync(1);

        Assert.Equal(new[] { "Baking", "Dairy" }, categories);
    }

    [Fact]
    public async Task AddItemAsync_PersistsIngredientAndMapsReturnedDto()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedReferenceData(dbContext);
        await dbContext.SaveChangesAsync(ct);

        var service = new InventoryManagementService(dbContext);
        var before = DateTime.UtcNow;

        var dto = await service.AddItemAsync(new CreateIngredientDTO
        {
            Name = "Butter",
            SKU = "BUT-1",
            CurrentQuantity = 8m,
            Price = 3.75m,
            Category = null,
            ReorderLevel = 2m,
            UnitId = 1,
            LocationId = 1,
            VendorId = 1,
            StorageLocationId = 1
        });
        var after = DateTime.UtcNow;

        var stored = await dbContext.Ingredients.SingleAsync(ct);
        Assert.Equal("Butter", stored.Name);
        Assert.Equal("BUT-1", stored.Sku);
        Assert.Equal(string.Empty, stored.Category);
        Assert.Equal(8m, stored.StockQuantity);
        Assert.True(stored.CreatedAt >= before && stored.CreatedAt <= after);

        Assert.Equal(stored.Id, dto.Id);
        Assert.Equal("Butter", dto.Name);
        Assert.Equal("BUT-1", dto.SKU);
        Assert.Equal(string.Empty, dto.Category);
        Assert.Equal(8m, dto.CurrentQuantity);
        Assert.False(dto.IsLowStock);
        Assert.Equal("Liter", dto.Unit);
    }

    [Fact]
    public async Task UpdateItemAsync_UpdatesMutableFieldsAndNormalizesBlankSku()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedReferenceData(dbContext);

        dbContext.Ingredients.Add(new Ingredient
        {
            Id = 1,
            Name = "Milk",
            LocationId = 1,
            UnitId = 1,
            VendorId = 1,
            StorageLocationId = 1,
            Category = "Dairy",
            StockQuantity = 5m,
            ReorderLevel = 2m,
            Price = 4.25m,
            Sku = "MILK-1"
        });
        await dbContext.SaveChangesAsync(ct);

        var service = new InventoryManagementService(dbContext);

        var updated = await service.UpdateItemAsync(new InventoryItemDTO
        {
            Id = 1,
            Name = "Skim Milk",
            SKU = "   ",
            Category = "Cold",
            CurrentQuantity = 1m,
            UnitId = 2,
            Price = 5m,
            ReorderLevel = 2m,
            VendorId = null,
            StorageLocationId = null
        });

        Assert.NotNull(updated);
        Assert.Equal("Skim Milk", updated!.Name);
        Assert.Equal(string.Empty, updated.SKU);
        Assert.Equal("Kilogram", updated.Unit);
        Assert.True(updated.IsLowStock);
        Assert.Null(updated.VendorId);
        Assert.Null(updated.StorageLocationId);

        var stored = await dbContext.Ingredients.SingleAsync(ct);
        Assert.Equal("Skim Milk", stored.Name);
        Assert.Null(stored.Sku);
        Assert.Equal("Cold", stored.Category);
        Assert.Equal(1m, stored.StockQuantity);
        Assert.Equal(2, stored.UnitId);
        Assert.Null(stored.VendorId);
        Assert.Null(stored.StorageLocationId);
    }

    [Fact]
    public async Task DeleteItemAsync_ReturnsTrueWhenItemExistsAndFalseOtherwise()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedReferenceData(dbContext);
        dbContext.Ingredients.Add(new Ingredient
        {
            Id = 1,
            Name = "Milk",
            LocationId = 1,
            UnitId = 1,
            Category = "Dairy"
        });
        await dbContext.SaveChangesAsync(ct);

        var service = new InventoryManagementService(dbContext);

        var deleted = await service.DeleteItemAsync(1);
        var missing = await service.DeleteItemAsync(999);

        Assert.True(deleted);
        Assert.False(missing);
        Assert.Empty(await dbContext.Ingredients.ToListAsync(ct));
    }

    private static AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedReferenceData(AppDbContext dbContext)
    {
        dbContext.Locations.AddRange(
            new Location
            {
                Id = 1,
                Name = "Main Kitchen",
                CompanyId = 1,
                Address = "123 Test St",
                City = "Testville",
                State = "IA",
                ZipCode = "50000"
            },
            new Location
            {
                Id = 2,
                Name = "Second Kitchen",
                CompanyId = 1,
                Address = "456 Test St",
                City = "Testville",
                State = "IA",
                ZipCode = "50001"
            });
        dbContext.Units.AddRange(
            new Unit { Id = 1, Name = "Liter", Abbreviation = "L", ConversionFactor = 1m },
            new Unit { Id = 2, Name = "Kilogram", Abbreviation = "kg", ConversionFactor = 1m });
        dbContext.StorageLocations.Add(new StorageLocation
        {
            Id = 1,
            LocationId = 1,
            Name = "Walk-in"
        });
        dbContext.Vendors.Add(new VendorEntity
        {
            Id = 1,
            Name = "Acme Supply",
            CompanyId = 1,
            LogoUrl = "https://example.com/logo.png"
        });
    }
}
