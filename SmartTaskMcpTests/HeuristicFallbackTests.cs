using CulinaryCommandSmartTaskMcp.Models;
using CulinaryCommandSmartTaskMcp.Services;
using Xunit;

namespace CulinaryCommandSmartTaskMcp.Tests
{
    public class HeuristicFallbackTests
    {
        private readonly HeuristicFallback _heuristics = new();
        private readonly PlanDefaults _planDefaults = new(30, 60);

        [Theory]
        [InlineData("Eggs Benedict", "Entree", null, "Breakfast")]
        [InlineData("Beef Bourguignon Stew", "Entree", null, "Dinner")]
        [InlineData("Caesar Salad", "Side", null, "AllDay")]
        [InlineData("Caesar Salad", "Side", "Lunch", "Lunch")]
        public void ClassifiesServiceWindowFromTitleAndOverride(
            string recipeTitle, string recipeType, string? overrideWindow, string expectedWindow)
        {
            var recipe = BuildRecipe(recipeTitle, recipeType, overrideWindow);
            Assert.Equal(expectedWindow, _heuristics.ClassifyServiceWindow(recipe));
        }

        [Fact]
        public void ParsesStepDurationsAndAddsBuffer()
        {
            var recipe = BuildRecipe("Soup", "Entree", null) with
            {
                Steps = new[]
                {
                    new RecipeStepInput(1, "8-10 minutes"),
                    new RecipeStepInput(2, "1.5 hr")
                }
            };

            var leadTimeMinutes = _heuristics.EstimatePrepLeadTimeMinutes(recipe, _planDefaults);

            Assert.Equal(10 + 90 + 30, leadTimeMinutes);
        }

        private static RecipeInput BuildRecipe(string title, string recipeType, string? serviceWindow) =>
            new(
                RecipeId: 1,
                Title: title,
                Category: "Test",
                RecipeType: recipeType,
                ServiceWindow: serviceWindow,
                ServiceTimeOverride: null,
                PrepLeadTimeMinutesOverride: null,
                Steps: Array.Empty<RecipeStepInput>(),
                SubRecipes: Array.Empty<RecipeInput>()
            );
    }
}