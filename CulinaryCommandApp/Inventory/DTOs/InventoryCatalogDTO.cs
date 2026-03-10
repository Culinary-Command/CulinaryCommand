using System;

namespace CulinaryCommand.Inventory.DTOs
{
    public class InventoryCatalogDTO
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = "count";
        public decimal PricePerUnit { get; set; }
        public decimal Supplier { get; set; }
        public string? Notes { get; set; }
    }
}
