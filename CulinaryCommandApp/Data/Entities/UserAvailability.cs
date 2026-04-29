using System.ComponentModel.DataAnnotations;

namespace CulinaryCommand.Data.Entities
{
    public class UserAvailability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public int LocationId { get; set; }
        public Location? Location { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeOnly ShiftStart { get; set; }

        [Required]
        public TimeOnly ShiftEnd { get; set; }
    }
}
