using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CulinaryCommand.Data.Enums;
using Rec = CulinaryCommandApp.Recipe.Entities;
using InvIngredient = CulinaryCommandApp.Inventory.Entities.Ingredient;

namespace CulinaryCommand.Data.Entities
{
    public class TaskTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string Station { get; set; } = string.Empty;

        public WorkTaskKind Kind { get; set; } = WorkTaskKind.Generic;

        public string Priority { get; set; } = "Normal";
        public string? Notes { get; set; }

        public int? DefaultEstimatedMinutes { get; set; }

        public int LocationId { get; set; }
        public Location? Location { get; set; }

        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? RecipeId { get; set; }
        public Rec.Recipe? Recipe { get; set; }

        public int? IngredientId { get; set; }
        public InvIngredient? Ingredient { get; set; }

        public int? Par { get; set; }
        public int? Count { get; set; }

        [NotMapped]
        public int Prep => (Par.HasValue && Count.HasValue)
            ? Math.Max(Par.Value - Count.Value, 0)
            : 0;

        public ICollection<TaskListItem> TaskListItems { get; set; } = new List<TaskListItem>();
    }
}