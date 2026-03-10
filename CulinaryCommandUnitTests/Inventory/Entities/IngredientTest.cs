using System;
using CulinaryCommand.Inventory.Entities;
using Xunit;

namespace CulinaryCommandUnitTests.Inventory.Entities
{
    public class IngredientTest
    {
        [Fact]
        public void constructor_sets_default_values()
        {
            var ingredient = new Ingredient();

            Assert.Equal(0, ingredient.Id);
            Assert.Equal(0, ingredient.UnitId);
            Assert.Equal(0m, ingredient.StockQuantity);
            Assert.Equal(0m, ingredient.ReorderLevel);
            Assert.Equal(string.Empty, ingredient.Name);
            Assert.Equal(string.Empty, ingredient.Category);

            Assert.Null(ingredient.CreatedAt);
            Assert.Null(ingredient.UpdatedAt);
            Assert.Null(ingredient.Price);
            Assert.Null(ingredient.Notes);
            Assert.Null(ingredient.Unit);
            Assert.Null(ingredient.Sku);
        }

        [Fact]
        public void properties_can_be_set_and_read_back()
        {
            var now = DateTime.UtcNow;
            var ingredient = new Ingredient();

            // set mock values
            ingredient.Id = 5;
            ingredient.Name = "Flour";
            ingredient.UnitId = 2;
            ingredient.StockQuantity = 10.5m;
            ingredient.ReorderLevel = 3m;
            ingredient.CreatedAt = now.AddDays(-1);
            ingredient.UpdatedAt = now;
            ingredient.Category = "Baking";
            ingredient.Sku = "1023";
            ingredient.Price = 4.99m;
            ingredient.Notes = "Store somewhere cool";

            // test values
            Assert.Equal(5, ingredient.Id);
            Assert.Equal("Flour", ingredient.Name);
            Assert.Equal(2, ingredient.UnitId);
            Assert.Equal(10.5m, ingredient.StockQuantity);
            Assert.Equal(3m, ingredient.ReorderLevel);
            Assert.Equal(now.AddDays(-1), ingredient.CreatedAt);
            Assert.Equal(now, ingredient.UpdatedAt);
            Assert.Equal("Baking", ingredient.Category);
            Assert.Equal("1023", ingredient.Sku);
            Assert.Equal(4.99m, ingredient.Price);
            Assert.Equal("Store somewhere cool", ingredient.Notes);
        }

        [Fact]
        public void allows_nullable_properties_to_be_null()
        {
            var ingredient = new Ingredient
            {
                Sku = null,
                Price = null,
                Notes = null,
                CreatedAt = null,
                UpdatedAt = null,
                Unit = null
            };

            Assert.Null(ingredient.Sku);
            Assert.Null(ingredient.Price);
            Assert.Null(ingredient.Notes);
            Assert.Null(ingredient.CreatedAt);
            Assert.Null(ingredient.UpdatedAt);
            Assert.Null(ingredient.Unit);
        }
    }
}