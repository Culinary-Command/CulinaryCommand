using System;
using System.Collections.Generic;

namespace CulinaryCommand.Inventory.Entities
{
    public class Unit
    {
        public int Id {get; set;}

        // nk (ex: "g", "kg", "L")
        public string Abbreviation {get; set; } = string.Empty;

        // full name (ex: "gram", "kilogram", "liter")
        public string Name { get; set; } = string.Empty;

        // multiplier to a base unit (ex: 1 for g, 1000 for kg if gram is base)
        public decimal ConversionFactor { get; set; }

        // list of ingredients that use unit as their base stock unit
        public ICollection<Ingredient> Ingredients {get; set;} = new List<Ingredient>();

        // list of inventory transactions that use this unit
        public ICollection<InventoryTransaction> InventoryTransaction { get; set; } = new List<InventoryTransaction>();

    }
}