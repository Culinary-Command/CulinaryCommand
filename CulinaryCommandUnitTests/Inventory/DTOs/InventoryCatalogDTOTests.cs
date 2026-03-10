using CulinaryCommand.Inventory.DTOs;
using Xunit;

namespace CulinaryCommandUnitTests.Inventory.DTOs
{
    public class InventoryCatalogDTOTests
    {
        [Fact]
        public void constructor_sets_default_values()
        {
            var DTO = new InventoryCatalogDTO();

            Assert.Equal(0, DTO.Id);
            Assert.Equal(string.Empty, DTO.ItemName);
            Assert.Equal(string.Empty, DTO.SKU);
            Assert.Equal(string.Empty, DTO.Category);
            Assert.Equal("count", DTO.Unit);
            Assert.Equal(0m, DTO.PricePerUnit);
            Assert.Equal(0m, DTO.Supplier);
            Assert.Null(DTO.Notes);
        }

        [Fact]
        public void properties_can_be_set_and_read_back()
        {
            var DTO = new InventoryCatalogDTO
            {
                Id = 7,
                ItemName = "Olive Oil",
                SKU = "654",
                Category = "Oils & Sauces",
                Unit = "L",
                PricePerUnit = 8.99m,
                Supplier = 3m,
                Notes = "Extra virgin"
            };

            Assert.Equal(7, DTO.Id);
            Assert.Equal("Olive Oil", DTO.ItemName);
            Assert.Equal("654", DTO.SKU);
            Assert.Equal("Oils & Sauces", DTO.Category);
            Assert.Equal("L", DTO.Unit);
            Assert.Equal(8.99m, DTO.PricePerUnit);
            Assert.Equal(3m, DTO.Supplier);
            Assert.Equal("Extra virgin", DTO.Notes);
        }

        [Fact]
        public void notes_can_be_null()
        {
            var DTO = new InventoryCatalogDTO
            {
                Notes = null
            };

            Assert.Null(DTO.Notes);
        }

        [Fact]
        public void notes_can_be_set_to_empty_string()
        {
            var DTO = new InventoryCatalogDTO
            {
                Notes = string.Empty
            };

            Assert.Equal(string.Empty, DTO.Notes);
        }

        [Fact]
        public void price_per_unit_accepts_decimal_precision()
        {
            var DTO = new InventoryCatalogDTO
            {
                PricePerUnit = 12.345m
            };

            Assert.Equal(12.345m, DTO.PricePerUnit);
        }

        [Fact]
        public void unit_default_is_count()
        {
            var DTO = new InventoryCatalogDTO();

            Assert.Equal("count", DTO.Unit);
        }

        [Fact]
        public void unit_can_be_overridden()
        {
            var DTO = new InventoryCatalogDTO
            {
                Unit = "lbs"
            };

            Assert.Equal("lbs", DTO.Unit);
        }
    }
}
