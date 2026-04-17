using System.ComponentModel.DataAnnotations;
using CulinaryCommand.Data.Enums;

namespace CulinaryCommand.Data.Models
{
    public class CreateTaskTemplateRequest : IValidatableObject
    {
        [StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(128)]
        public string Station { get; set; } = "Prep";

        public WorkTaskKind Kind { get; set; } = WorkTaskKind.Generic;

        [Required]
        public string Priority { get; set; } = "Normal";

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Range(1, 1440)]
        public int? DefaultEstimatedMinutes { get; set; }

        [Required]
        public int LocationId { get; set; }

        public int? CreatedByUserId { get; set; }

        public int? RecipeId { get; set; }
        public int? IngredientId { get; set; }

        public int? Par { get; set; }
        public int? Count { get; set; }

        public int PrepNeeded => Math.Max((Par ?? 0) - (Count ?? 0), 0);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Kind == WorkTaskKind.PrepFromRecipe)
            {
                if (!RecipeId.HasValue)
                {
                    yield return new ValidationResult(
                        "A recipe is required for prep-from-recipe templates.",
                        new[] { nameof(RecipeId) });
                }
            }
            else if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult(
                    "Template name is required.",
                    new[] { nameof(Name) });
            }
        }
    }
}
