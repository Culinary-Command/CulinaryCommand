using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CulinaryCommand.Data.Enums;
using InvIngredient = CulinaryCommand.Inventory.Entities.Ingredient;

namespace CulinaryCommand.Data.Entities
{
    public class Tasks
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string? Name { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string? Station { get; set; } = string.Empty;

        [Required]
        public string? Status { get; set; }

        [Required]
        public string? Assigner { get; set; }

        [Required]
        public DateTime? Date { get; set; }

        // Optional: assign to a user
        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.UtcNow;

        public int LocationId { get; set; }
        public Location? Location { get; set; }

       
        // Task Kind (Generic or Prep)
        public WorkTaskKind Kind { get; set; } = WorkTaskKind.Generic;

        
        // Prep Task Fields 
        

        // Link to recipe if this is a prep task
        public int? RecipeId { get; set; }
        public Recipe? Recipe { get; set; }

        // Link to ingredient (Inventory.Entities.Ingredient)
        public int? IngredientId { get; set; }
        public InvIngredient? Ingredient { get; set; }

        // Par / Count for prep task
        public int? Par { get; set; }
        public int? Count { get; set; }

        // Computed property for UI
        [NotMapped]
        public int Prep => (Par.HasValue && Count.HasValue)
            ? Math.Max(Par.Value - Count.Value, 0)
            : 0;

       
        // Extra
        public string Priority { get; set; } = "Normal";
        public string? Notes { get; set; }

    }
}