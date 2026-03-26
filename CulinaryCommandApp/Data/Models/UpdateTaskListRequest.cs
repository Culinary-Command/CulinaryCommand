using System.ComponentModel.DataAnnotations;

namespace CulinaryCommand.Data.Models
{
    public class UpdateTaskListRequest
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int LocationId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}