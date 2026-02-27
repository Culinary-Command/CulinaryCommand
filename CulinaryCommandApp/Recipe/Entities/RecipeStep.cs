using System.ComponentModel.DataAnnotations;

namespace CulinaryCommandApp.Recipe.Entities
{
    public class RecipeStep
    {
        [Key]
        public int StepId { get; set; }

        public int RecipeId { get; set; }

        public int StepNumber { get; set; }

        [MaxLength(2048)]
        public string Instructions { get; set; } = string.Empty;
                            
        // optional: estimated duration (ex: "5 minutes", "8-10 minutes")
        [MaxLength(64)]
        public string? Duration { get; set; }

        // optional: target temperature (ex: "375-400°F", "Internal: 145°F")
        [MaxLength(64)]
        public string? Temperature { get; set; }

        // optional: equipment or tools needed (ex: "Gas or charcoal grill", "Spatula")
        [MaxLength(256)]
        public string? Equipment { get; set; }

        // Navigation
        public Recipe? Recipe { get; set; }
    }
}