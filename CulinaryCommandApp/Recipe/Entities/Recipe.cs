using System.ComponentModel.DataAnnotations;
using CulinaryCommand.Data.Entities;

namespace CulinaryCommandApp.Recipe.Entities
{
    public class Recipe
    {
        [Key]
        public int RecipeId { get; set; }

        public int LocationId { get; set; }
        public Location? Location { get; set; }

        [Required, MaxLength(128)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(128)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(128)]
        public string RecipeType { get; set; } = string.Empty;

        [MaxLength(128)]
        public string YieldUnit { get; set; } = string.Empty;

        public decimal? YieldAmount { get; set; }

        public decimal? CostPerYield { get; set; }

        public bool IsSubRecipe { get; set; } = false;

        public DateTime? CreatedAt { get; set; }

        // Required to handle concurrent changes
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
        public ICollection<RecipeSubRecipe> SubRecipeUsages { get; set; } = new List<RecipeSubRecipe>();
        public ICollection<RecipeSubRecipe> UsedInRecipes { get; set; } = new List<RecipeSubRecipe>();
    }
}