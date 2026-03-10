using System;
using System.Linq;
using CulinaryCommand.Inventory.Entities;
using Xunit;

namespace CulinaryCommandUnitTests.Inventory.Entities
{
    public class UnitTests
    {
        [Fact]
        public void constructor_sets_default_values()
        {
            var unit = new Unit();

            Assert.NotNull(unit.Ingredients);
            Assert.NotNull(unit.InventoryTransaction);
            Assert.Equal(0, unit.Id);
            Assert.Equal(string.Empty, unit.Abbreviation);
            Assert.Equal(string.Empty,unit.Name);
            Assert.Equal(0m, unit.ConversionFactor);
            Assert.Empty(unit.Ingredients);
            Assert.Empty(unit.InventoryTransaction);
        }

        [Fact]
        public void properties_can_be_set_and_read_back()
        {
            var unit = new Unit();
            unit.Id = 1;
            unit.Abbreviation = "g";
            unit.Name = "gram";
            unit.ConversionFactor = 1m;

            var ingredient = new Ingredient{ Id = 5, Name = "Flour", UnitId = unit.Id, StockQuantity = 100m, ReorderLevel = 10m, Category = "Baking" };
            unit.Ingredients.Add(ingredient);

            var transaction = new InventoryTransaction
            {
                Id = 100,
                IngredientId = ingredient.Id,
                StockChange = -2.5m,
                UnitId = unit.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = "Usage",
                Ingredient = ingredient,
                Unit = unit
            };
            unit.InventoryTransaction.Add(transaction);

            Assert.Equal(1, unit.Id);
            Assert.Equal("g", unit.Abbreviation);
            Assert.Equal("gram", unit.Name);
            Assert.Equal(1m, unit.ConversionFactor);
            Assert.Single(unit.Ingredients);
            Assert.Single(unit.InventoryTransaction);
            Assert.Same(ingredient, unit.Ingredients.First());
            Assert.Same(transaction, unit.InventoryTransaction.First());
        }

        [Fact]
        public void collections_are_initialized_independently()
        {
            var a = new Unit();
            var b = new Unit();

            Assert.NotSame(a.Ingredients, b.Ingredients);
            Assert.NotSame(a.InventoryTransaction, b.InventoryTransaction);

            a.Ingredients.Add(new Ingredient { Id = 1, Name = "Flour "});
            Assert.Single(a.Ingredients);
            Assert.Empty(b.Ingredients);
        }
    }
}