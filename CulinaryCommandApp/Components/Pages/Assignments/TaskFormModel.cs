using System.ComponentModel.DataAnnotations;
using CulinaryCommand.Data.Enums;

namespace CulinaryCommandApp.Components.Pages.Assignments;

public class TaskFormModel : IValidatableObject
{
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Station { get; set; } = "Prep";

    [Required]
    public string Priority { get; set; } = "Normal";

    public int? UserId { get; set; }

    [Required]
    public DateTime? DueDate { get; set; } = DateTime.Today;

    [StringLength(512)]
    public string? Notes { get; set; } = string.Empty;

    public WorkTaskKind TaskType { get; set; } = WorkTaskKind.Generic;

    public int? Par { get; set; }
    public int? Count { get; set; }

    public int? RecipeId { get; set; }

    public int PrepNeeded => Math.Max((Par ?? 0) - (Count ?? 0), 0);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TaskType == WorkTaskKind.PrepFromRecipe)
        {
            if (!RecipeId.HasValue)
            {
                yield return new ValidationResult(
                    "A recipe is required for prep-from-recipe tasks.",
                    new[] { nameof(RecipeId) });
            }
        }
        else if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Task name is required.",
                new[] { nameof(Name) });
        }
    }
}
