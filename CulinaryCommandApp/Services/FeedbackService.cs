using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryCommand.Services
{
    public interface IFeedbackService
    {
        Task<Feedback> SubmitFeedbackAsync(Feedback feedback);
        Task<List<Feedback>> GetAllFeedbackAsync();
        Task<List<Feedback>> GetFeedbackByTypeAsync(string feedbackType);
        Task<List<Feedback>> GetFeedbackByUserAsync(int userId);
        Task<Feedback?> GetFeedbackByIdAsync(int id);
        Task<bool> DeleteFeedbackAsync(int id);
    }

    public class FeedbackService : IFeedbackService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public FeedbackService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        private AppDbContext CreateDb() =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

        public async Task<Feedback> SubmitFeedbackAsync(Feedback feedback)
        {
            using var db = CreateDb();
            feedback.SubmittedAt = DateTime.UtcNow;
            db.Feedbacks.Add(feedback);
            await db.SaveChangesAsync();
            return feedback;
        }

        public async Task<List<Feedback>> GetAllFeedbackAsync()
        {
            using var db = CreateDb();
            return await db.Feedbacks
                .Include(f => f.User)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<Feedback>> GetFeedbackByTypeAsync(string feedbackType)
        {
            using var db = CreateDb();
            return await db.Feedbacks
                .Include(f => f.User)
                .Where(f => f.FeedbackType == feedbackType)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<Feedback>> GetFeedbackByUserAsync(int userId)
        {
            using var db = CreateDb();
            return await db.Feedbacks
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();
        }

        public async Task<Feedback?> GetFeedbackByIdAsync(int id)
        {
            using var db = CreateDb();
            return await db.Feedbacks
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<bool> DeleteFeedbackAsync(int id)
        {
            using var db = CreateDb();
            var feedback = await db.Feedbacks.FindAsync(id);
            if (feedback == null) return false;

            db.Feedbacks.Remove(feedback);
            await db.SaveChangesAsync();
            return true;
        }
    }
}