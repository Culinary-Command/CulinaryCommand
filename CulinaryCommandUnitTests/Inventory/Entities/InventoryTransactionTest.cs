using System;
using CulinaryCommand.Inventory.Entities;
using Xunit;

namespace CulinaryCommandUnitTests.Inventory.Entities
{
    public class InventoryTransactionTests
    {
        [Fact]
        public void constructor_sets_defaults_values()
        {
            var transaction = new InventoryTransaction();

            Assert.Equal(0, transaction.Id);
            Assert.Equal(0, transaction.IngredientId);
            Assert.Equal(0m, transaction.StockChange);
            Assert.Equal(0, transaction.UnitId);
            Assert.Equal(default(DateTimeOffset), transaction.CreatedAt);
            Assert.Equal(string.Empty, transaction.Reason);
            Assert.Null(transaction.Ingredient);
        }

        [Fact]
        public void properties_can_be_set_and_read_back()
        {
            var now = DateTimeOffset.UtcNow;
            var transaction = new InventoryTransaction
            {
                Id = 10,
                IngredientId = 5,
                StockChange = -2.5m,
                UnitId = 3,
                CreatedAt = now,
                Reason = "Usage",
                Ingredient = new Ingredient { Id = 5, Name = "Flour", UnitId = 1 },
                Unit = new Unit { Id = 3, Name = "gram", Abbreviation = "g", ConversionFactor = 1m }
            };

            Assert.NotNull(transaction.Ingredient);
            Assert.NotNull(transaction.Unit);
            Assert.Equal(10, transaction.Id);
            Assert.Equal(5, transaction.IngredientId);
            Assert.Equal(-2.5m, transaction.StockChange);
            Assert.Equal(3, transaction.UnitId);
            Assert.Equal(now, transaction.CreatedAt);
            Assert.Equal("Usage", transaction.Reason);
            Assert.Equal(5, transaction.Ingredient.Id);
            Assert.Equal(3, transaction.Unit.Id);
        }

        [Fact]
        public void ingredient_and_unit_can_be_null()
        {
            var transaction = new InventoryTransaction
            {
                Ingredient = null,
                Unit = null,
            };

            Assert.Null(transaction.Ingredient);
            Assert.Null(transaction.Unit);
        }
    }
}