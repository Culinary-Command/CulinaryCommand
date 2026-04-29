using CulinaryCommand.Data.Entities;

namespace CulinaryCommandApp.SmartTask.Services.Interfaces
{
    public interface ISmartTaskService
    {
        Task<SmartTaskPlanPreview> PreviewAsync(
            SmartTaskRequest request, CancellationToken cancellationToken);

        Task<SmartTaskRun> CommitAsync(Guid planPreviewId, CancellationToken cancellationToken);

        Task RollbackAsync(Guid runId, CancellationToken cancellationToken);

        Task<List<SmartTaskRun>> RecentRunsAsync(int locationId, int take = 10);
    }

    public sealed record SmartTaskRequest(
        int LocationId,
        DateOnly ServiceDate,
        IReadOnlyList<int> RecipeIds,
        int TriggeredByUserId);

    public sealed class SmartTaskPlanPreview
    {
        public Guid PreviewId { get; init; } = Guid.NewGuid();
        public required SmartTaskRequest OriginalRequest { get; init; }
        public required PlanResponseDto Plan { get; init; }
        public DateTime ExpiresAtUtc { get; init; } = DateTime.UtcNow.AddMinutes(10);
    }
}