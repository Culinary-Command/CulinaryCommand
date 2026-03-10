using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CulinaryCommand.Data.Entities
{
    public class RecipeStep
    {
        [Key]
        public int StepId { get; set; }
        public int RecipeId { get; set; }

        public int StepNumber { get; set; } // 1, 2, 3...

        [MaxLength(256)]
        public string? Instructions { get; set; }

        // Navigation
        public Recipe? Recipe { get; set; }
    }

}