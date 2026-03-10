using System;

namespace CulinaryCommand.Inventory.Entities
{
    
    public class InventoryTransaction
    {
        public int Id { get; set; }

        // ingredient whose stock is being changed.
        public int IngredientId { get; set; }

        // amount of stock being changed.
        public decimal StockChange { get; set; }

        // Unit of measurement for this transaction.
        public int UnitId { get; set; }

        // when the transaction occurred 
        public DateTimeOffset CreatedAt { get; set; }

        // simple reason for the transaction (ex: restock, usage, went bad)
        // can be replaced with an enum later if we want
        public string Reason { get; set; } = string.Empty;

        // the ingredient that is added/removed from inventory
        public Ingredient? Ingredient { get; set; }

        // unit used for transaction
        public Unit? Unit { get; set; }
    }
}