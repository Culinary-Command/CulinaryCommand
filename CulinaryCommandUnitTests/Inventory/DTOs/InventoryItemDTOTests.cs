using System;
using CulinaryCommand.Inventory.DTOs;
using Xunit;

namespace CulinaryCommandUnitTests.Inventory.DTOs
{

    public class InventoryItemDTOTests {

        [Fact]
        public void constructor_sets_default_values()
        {
            var DTO = new InventoryItemDTO();

            Assert.Null(DTO.OutOfStockDate);
            Assert.Null(DTO.LastOrderDate);
            Assert.Null(DTO.Notes);
            Assert.Equal(0, DTO.Id);
            Assert.Equal(string.Empty, DTO.Name);
            Assert.Equal(string.Empty, DTO.SKU);
            Assert.Equal(string.Empty, DTO.Category);
            Assert.Equal(0m, DTO.CurrentQuantity);
            Assert.Equal("count", DTO.Unit);
            Assert.Equal(0m, DTO.Price);
            Assert.False(DTO.IsLowStock);
        }

        [Fact]
        public void properties_can_be_set_and_read_back()
        {
            var now = DateTime.UtcNow;
            var out_of_stock = now.AddDays(-2);
            var last_order = now.AddDays(-1);

            var DTO = new InventoryItemDTO
            {
                Id = 42,
                Name = "Flour",
                SKU = "9203",
                Category = "Baking",
                CurrentQuantity = 5.5m,
                Unit = "kg",
                Price = 3.99m,
                ReorderLevel = 2m,
                IsLowStock = true,
                OutOfStockDate = out_of_stock,
                LastOrderDate = last_order,
                Notes = "Keep cool"
            };
            
            Assert.Equal(42, DTO.Id);
            Assert.Equal("Flour", DTO.Name);
            Assert.Equal("9203", DTO.SKU);
            Assert.Equal("Baking", DTO.Category);
            Assert.Equal(5.5m, DTO.CurrentQuantity);
            Assert.Equal("kg", DTO.Unit);
            Assert.Equal(3.99m, DTO.Price);
            Assert.Equal(2m, DTO.ReorderLevel);
            Assert.Equal(out_of_stock, DTO.OutOfStockDate);
            Assert.Equal(last_order, DTO.LastOrderDate);
            Assert.Equal("Keep cool", DTO.Notes);
        }

        [Fact]
        public void nullable_properties_can_be_null()
        {
            var DTO = new InventoryItemDTO
            {
                OutOfStockDate = null,
                LastOrderDate = null,
                Notes = null
            };

            Assert.Null(DTO.OutOfStockDate);
            Assert.Null(DTO.LastOrderDate);
            Assert.Null(DTO.Notes);
        }
    }
}