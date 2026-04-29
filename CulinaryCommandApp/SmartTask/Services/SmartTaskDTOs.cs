namespace CulinaryCommandApp.SmartTask.Services
{
    public sealed record PlanRequestDto(
        int LocationId,
        DateOnly ServiceDate,
        IReadOnlyList<RecipeInputDto> Recipes,
        IReadOnlyList<EligibleUserDto> EligibleUsers,
        PlanDefaultsDto Defaults
    );

    public sealed record RecipeInputDto(
        int RecipeId,
        string Title,
        string Category,
        string RecipeType,
        string? ServiceWindow,
        TimeOnly? ServiceTimeOverride,
        int? PrepLeadTimeMinutesOverride,
        IReadOnlyList<RecipeStepInputDto> Steps,
        IReadOnlyList<RecipeInputDto> SubRecipes
    );

    public sealed record RecipeStepInputDto(int StepNumber, string? Duration);

    public sealed record EligibleUserDto(int UserId, string DisplayName, int OpenTaskCountToday);

    public sealed record PlanDefaultsDto(int DefaultPrepBufferMinutes, int DefaultLeadTimeWhenUnknown);

    public sealed record PlanResponseDto(IReadOnlyList<PlannedPrepTaskDto> PlannedTasks);

    public sealed record PlannedPrepTaskDto(
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