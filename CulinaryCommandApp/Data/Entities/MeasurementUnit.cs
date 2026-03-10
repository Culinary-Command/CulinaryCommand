using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CulinaryCommand.Data.Entities
{
    public class MeasurementUnit
    {
        [Key]
        public int UnitId { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; }   // "Cup"
        
        [Required, MaxLength(32)]
        public string Abbreviation { get; set; } // "c"

        // Navigation
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }

}