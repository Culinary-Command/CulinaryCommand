using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using CulinaryCommand.Inventory.Entities;

namespace CulinaryCommand.Data.Entities
{
    public class RecipeIngredient
    {
        [Key]
        public int RecipeIngredientId { get; set; }
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public int UnitId { get; set; }

        public decimal Quantity { get; set; }

        [MaxLength(256)]
        public string? PrepNote { get; set; }
        public int SortOrder { get; set; }

        // Navigation
        public Recipe? Recipe { get; set; }
        public CulinaryCommand.Inventory.Entities.Ingredient? Ingredient { get; set; }
        public Unit? Unit { get; set; }

        [NotMapped]
        public List<CulinaryCommand.Inventory.Entities.Ingredient> AvailableIngredients { get; set; } = new();

        [NotMapped]
        public List<Unit> AvailableUnits { get; set; } = new();
    }

}