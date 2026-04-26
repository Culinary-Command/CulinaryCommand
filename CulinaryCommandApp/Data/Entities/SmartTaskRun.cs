using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CulinaryCommand.Data.Entities
{
    public enum SmartTaskRunStatus
    {
        Planned = 0,
        Committed = 1,
        RolledBack = 2,
        Failed = 3
    }

    public class SmartTaskRun
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public int LocationId { get; set; }
        public Location? Location { get; set; }

        [Required]
        public int TriggeredByUserId { get; set; }
        public User? TriggeredByUser { get; set; }

        [Required]
        public DateOnly ServiceDate { get; set; }

        [Required, Column(TypeName = "TEXT")]
        public string RecipeIdsJson { get; set; } = "[]";

        [Required, Column(TypeName = "TEXT")]
        public string CreatedTaskIdsJson { get; set; } = "[]";

        [MaxLength(64)]
        public string? LambdaRequestId { get; set; }

        [Required, MaxLength(32)]
        public string Status { get; set; } = nameof(SmartTaskRunStatus.Planned);

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}