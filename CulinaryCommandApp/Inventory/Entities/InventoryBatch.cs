// Allow the same ingredient to exist with multiple expiration dates and quantities.

using CulinaryCommand.Data.Entities;

namespace CulinaryCommand.Inventory.Entities
{
    public class InventoryBatch
    {
        public int Id { get; set; }

        public int LocationId { get; set; }
        public Location Location { get; set; } = default!;

        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; } = default!;

        public DateTime ExpirationDate { get; set; }
        
        public decimal Quantity { get; set; }
        public Unit? Unit { get; set; }

        // auditing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedByUserId { get; set; }

        // link to Product Oder line for traceability
        public int? PurchaseOrderId { get; set; }
        public int? PurchaseOrderLineId { get; set; }
    }

}