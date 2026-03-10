// Represents a single ingredient on a purchase order
using CulinaryCommand.Inventory.Entities;

namespace CulinaryCommand.PurchaseOrder.Entities
{
    public class PurchaseOrderLine
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = default!;

        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; } = default!;

        // FK to unit used for this PO line
        public int UnitId { get; set; }
        public Unit? Unit { get; set; }

        public decimal QuantityOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal QuantityReceived { get; set; }
    }
}