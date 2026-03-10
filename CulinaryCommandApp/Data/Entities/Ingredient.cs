using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CulinaryCommand.Data.Entities
{
    public class Ingredient
    {
        [Key]
        public int IngredientId { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; }

        [Required, MaxLength(128)]
        public string Category { get; set; }

        [Required, MaxLength(128)]
        public string DefaultUnit { get; set; }

        // Navigation
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}