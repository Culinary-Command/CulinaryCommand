using System.ComponentModel.DataAnnotations;
using CulinaryCommandApp.Inventory.Entities;

namespace CulinaryCommandApp.Recipe.Entities
{
    public class RecipeIngredient
    {
        [Key]
        public int RecipeIngredientId { get; set; }

        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }

        public int? IngredientId { get; set; }
        public Ingredient? Ingredient { get; set; }

        public int? SubRecipeId { get; set; }
        public Recipe? SubRecipe { get; set; }

        public int UnitId { get; set; }
        public Unit? Unit { get; set; }

        public int SortOrder { get; set; }

        public decimal Quantity { get; set; }

        [MaxLength(256)]
        public string? PrepNote { get; set; }
    }
}