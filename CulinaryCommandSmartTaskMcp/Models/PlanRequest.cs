namespace CulinaryCommandSmartTaskMcp.Models
{
    public sealed record PlanRequest(
        int LocationId,
        DateOnly ServiceDate,
        IReadOnlyList<RecipeInput> Recipes,
        IReadOnlyList<EligibleUser> EligibleUsers,
        PlanDefaults Defaults
    );

    public sealed record RecipeInput(
        int RecipeId,
        string Title,
        string Category,
        string RecipeType,
        string? ServiceWindow,
        TimeOnly? ServiceTimeOverride,
        int? PrepLeadTimeMinutesOverride,
        IReadOnlyList<RecipeStepInput> Steps,
        IReadOnlyList<RecipeInput> SubRecipes
    );

    public sealed record RecipeStepInput(int StepNumber, string? Duration);

    public sealed record EligibleUser(int UserId, string DisplayName, int OpenTaskCountToday);

    public sealed record PlanDefaults(int DefaultPrepBufferMinutes, int DefaultLeadTimeWhenUnknown);
}