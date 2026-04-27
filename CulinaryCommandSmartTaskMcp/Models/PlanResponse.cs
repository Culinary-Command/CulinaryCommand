namespace CulinaryCommandSmartTaskMcp.Models
{
    public sealed record PlanResponse(IReadOnlyList<PlannedPrepTask> PlannedTasks);

    public sealed record PlannedPrepTask(
        int RecipeId,
        string RecipeTitle,
        int AssignedUserId,
        DateTime DueDateUtc,
        int LeadTimeMinutes,
        string Priority,
        string ServiceWindow,
        string ReasoningSummary
    );
}