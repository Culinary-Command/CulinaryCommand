using System.ComponentModel.DataAnnotations;
using CulinaryCommand.Data.Enums;

namespace CulinaryCommandApp.Components.Pages.Assignments;

public class TaskFormModel
{
    [Required, StringLength(128)]
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
}
