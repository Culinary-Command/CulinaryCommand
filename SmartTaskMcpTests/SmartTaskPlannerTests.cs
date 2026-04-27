using CulinaryCommandSmartTaskMcp.Models;
using CulinaryCommandSmartTaskMcp.Services;
using Xunit;

namespace CulinaryCommandSmartTaskMcp.Tests
{
    public class SmartTaskPlannerTests
    {
        private readonly SmartTaskPlanner _planner = new(new HeuristicFallback(), new ServiceWindowClock());

        [Fact]
        public void DistributesTasksToLeastLoadedUserFirst()
        {
            var request = new PlanRequest(
                LocationId: 1,
                ServiceDate: new DateOnly(2026, 5, 1),
                Recipes: new[]
                {
                    BuildRecipe(1, "Eggs Benedict"),
                    BuildRecipe(2, "Avocado Toast"),
                    BuildRecipe(3, "French Toast")
                },
                EligibleUsers: new[]
                {
                    new EligibleUser(10, "Alice", OpenTaskCountToday: 0),
                    new EligibleUser(11, "Bob",   OpenTaskCountToday: 0)
                },
                Defaults: new PlanDefaults(30, 60));

            var response = _planner.Plan(request);

            var aliceTaskCount = response.PlannedTasks.Count(t => t.AssignedUserId == 10);
            var bobTaskCount   = response.PlannedTasks.Count(t => t.AssignedUserId == 11);

            Assert.Equal(3, response.PlannedTasks.Count);
            Assert.True(Math.Abs(aliceTaskCount - bobTaskCount) <= 1,
                "Tasks should be distributed evenly between users.");
        }

        [Fact]
        public void ThrowsWhenNoEligibleUsersSupplied()
        {
            var request = new PlanRequest(
                LocationId: 1,
                ServiceDate: new DateOnly(2026, 5, 1),
                Recipes: new[] { BuildRecipe(1, "Eggs Benedict") },
                EligibleUsers: Array.Empty<EligibleUser>(),
                Defaults: new PlanDefaults(30, 60));

            Assert.Throws<InvalidOperationException>(() => _planner.Plan(request));
        }

        [Fact]
        public void BreakfastRecipeDueDateIsBeforeBreakfastServiceWindow()
        {
            var request = new PlanRequest(
                LocationId: 1,
                ServiceDate: new DateOnly(2026, 5, 1),
                Recipes: new[] { BuildRecipe(1, "Eggs Benedict") },
                EligibleUsers: new[] { new EligibleUser(10, "Alice", 0) },
                Defaults: new PlanDefaults(30, 60));

            var response = _planner.Plan(request);
            var plannedTask = response.PlannedTasks.Single();

            var breakfastServiceStartUtc = new DateTime(2026, 5, 1, 6, 0, 0, DateTimeKind.Utc);
            Assert.True(plannedTask.DueDateUtc < breakfastServiceStartUtc,
                "Breakfast prep task due date must be before 6:00 AM service start.");
        }

        private static RecipeInput BuildRecipe(int recipeId, string title) =>
            new(recipeId, title, "Brunch", "Entree",
                ServiceWindow: null, ServiceTimeOverride: null,
                PrepLeadTimeMinutesOverride: null,
                Steps: Array.Empty<RecipeStepInput>(),
                SubRecipes: Array.Empty<RecipeInput>());
    }
}