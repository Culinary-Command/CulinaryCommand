using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CulinaryCommand.Data.Entities
{
    public class Recipe
    {
        [Key]
        public int RecipeId { get; set; }
        public int LocationId { get; set; }

        [Required, MaxLength(128)]
        public string? Title { get; set; }

        [Required, MaxLength(128)]
        public string Category { get; set; }

        [Required, MaxLength(128)]
        public string RecipeType { get; set; }

        public decimal? YieldAmount { get; set; }

        [MaxLength(128)]
        public string YieldUnit { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Location? Location { get; set; }
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    }

}