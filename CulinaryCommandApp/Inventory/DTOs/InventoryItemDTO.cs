using System;

namespace CulinaryCommand.Inventory.DTOs
{
    public class InventoryItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public string Unit { get; set; } = "count";
        public decimal Price { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsLowStock { get; set; }
        public DateTime? OutOfStockDate { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public string? Notes { get; set; }
        public int? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? VendorLogoUrl { get; set; }
    }
}
