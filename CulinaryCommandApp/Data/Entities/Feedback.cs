using System.ComponentModel.DataAnnotations.Schema;

namespace CulinaryCommand.Data.Entities
{
    public class Feedback
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public string FeedbackType { get; set; } = string.Empty; // "Bug", "Feature", "General"
        public string Page { get; set; } = string.Empty;
        public string? Device { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
        [Column(TypeName = "LONGTEXT")]
        public string? ScreenshotBase64 { get; set; }

        public User? User { get; set; }
    }
}