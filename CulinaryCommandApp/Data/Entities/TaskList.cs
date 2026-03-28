using System.ComponentModel.DataAnnotations;

namespace CulinaryCommand.Data.Entities
{
    public class TaskList
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? Description { get; set; }

        public int LocationId { get; set; }
        public Location? Location { get; set; }

        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaskListItem> Items { get; set; } = new List<TaskListItem>();
    }
}