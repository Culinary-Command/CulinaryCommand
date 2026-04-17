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

        // Optimistic concurrency token — backed by a MySQL timestamp(6) column
        // (ON UPDATE CURRENT_TIMESTAMP(6)), materialised as DateTime by the Pomelo provider.
        // ValueGeneratedOnAddOrUpdate + IsConcurrencyToken are set via fluent API in AppDbContext.
        public DateTime RowVersion { get; set; }

        // Navigation
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
        public ICollection<RecipeSubRecipe> SubRecipeUsages { get; set; } = new List<RecipeSubRecipe>();
        public ICollection<RecipeSubRecipe> UsedInRecipes { get; set; } = new List<RecipeSubRecipe>();
    }
}