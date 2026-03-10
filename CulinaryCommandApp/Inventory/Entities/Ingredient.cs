using System;
using CulinaryCommand.Data.Entities;

namespace CulinaryCommand.Inventory.Entities
{
    public class Ingredient
    {
        public int Id {get; set;}

        // display name of the ingredient (ex: "flour", "eggs")
        public string Name { get; set; } = string.Empty;

        // fk to the location this ingredient belongs to
        public int LocationId { get; set; }
        public Location? Location { get; set; }

        // optional fk to the vendor that supplies this ingredient
        public int? VendorId { get; set; }
        public CulinaryCommand.Vendor.Entities.Vendor? Vendor { get; set; }

        // fk to the unit used to track stock (ex: g, kg, each).
        public int UnitId { get; set; }

        // how much is currently in inventory, stored in the ingredient's base unit
        public decimal StockQuantity { get; set; }

        // reorder level used to determine if restocking is necessary
        public decimal ReorderLevel { get; set; } = 0m;

        // timestamp when the ingredient was created
        public DateTime? CreatedAt { get; set; }

        // optional timestamp when the ingredient was last updated
        public DateTime? UpdatedAt { get; set; }

        // the unit in which this ingredient's stock is tracked
        public Unit? Unit { get; set; }

        // category for the ingredient (ex: "dairy", "baking")
        public string Category { get; set; } = string.Empty;

        // optional, used to store SKU for ingredient
        public string? Sku { get; set; }

        // optional, used to store price for ingredient
        public decimal? Price { get; set; }

        // optional, used to describe any details about the ingredient
        public string? Notes { get; set; }
    }
}