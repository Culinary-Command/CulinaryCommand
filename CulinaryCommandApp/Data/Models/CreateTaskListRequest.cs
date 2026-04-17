using System.ComponentModel.DataAnnotations;

namespace CulinaryCommand.Data.Models
{
    public class CreateTaskListRequest
    {
        [Required, StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int LocationId { get; set; }

        public int? CreatedByUserId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}