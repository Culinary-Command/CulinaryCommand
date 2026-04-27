using System.Text.RegularExpressions;
using CulinaryCommandSmartTaskMcp.Models;

namespace CulinaryCommandSmartTaskMcp.Services
{
    public sealed class HeuristicFallback
    {
        private static readonly Regex BreakfastTitleRegex =
            new(@"\b(pancake|waffle|omelet|benedict|breakfast|granola|bagel)\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DinnerTitleRegex =
            new(@"\b(steak|braise|roast|stew|risotto|pizza|lasagna)\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DurationMinutesRegex =
            new(@"(\d+(?:\.\d+)?)(?:\s*[-–]\s*(\d+(?:\.\d+)?))?\s*(min|minute|hr|hour)s?",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string ClassifyServiceWindow(RecipeInput recipe)
        {
            if (!string.IsNullOrWhiteSpace(recipe.ServiceWindow))
                return recipe.ServiceWindow!;

            if (string.Equals(recipe.RecipeType, "Dessert", StringComparison.OrdinalIgnoreCase))
                return "AllDay";

            if (BreakfastTitleRegex.IsMatch(recipe.Title))
                return "Breakfast";

            if (DinnerTitleRegex.IsMatch(recipe.Title))
                return "Dinner";

            return "AllDay";
        }

        public int EstimatePrepLeadTimeMinutes(RecipeInput recipe, PlanDefaults defaults)
        {
            if (recipe.PrepLeadTimeMinutesOverride.HasValue)
                return recipe.PrepLeadTimeMinutesOverride.Value;

            var stepMinutesTotal = recipe.Steps
                .Select(step => ParseDurationToMinutesOrZero(step.Duration))
                .Sum();

            var subRecipeMinutesTotal = recipe.SubRecipes
                .Select(subRecipe => EstimatePrepLeadTimeMinutes(subRecipe, defaults))
                .Sum();

            var rawLeadTime = stepMinutesTotal + subRecipeMinutesTotal;

            return rawLeadTime == 0
                ? defaults.DefaultLeadTimeWhenUnknown + defaults.DefaultPrepBufferMinutes
                : rawLeadTime + defaults.DefaultPrepBufferMinutes;
        }

        public string SuggestPriority(int leadTimeMinutes, int subRecipeCount)
        {
            if (leadTimeMinutes >= 240 || subRecipeCount >= 3) return "High";
            if (leadTimeMinutes >= 90) return "Normal";
            return "Low";
        }

        private static int ParseDurationToMinutesOrZero(string? rawDuration)
        {
            if (string.IsNullOrWhiteSpace(rawDuration)) return 0;

            var match = DurationMinutesRegex.Match(rawDuration);
            if (!match.Success) return 0;

            var lowerBoundValue = double.Parse(match.Groups[1].Value);
            var upperBoundValue = match.Groups[2].Success
                ? double.Parse(match.Groups[2].Value)
                : lowerBoundValue;

            var unitToken = match.Groups[3].Value.ToLowerInvariant();
            var minutesPerUnit = unitToken.StartsWith("hr") || unitToken.StartsWith("hour") ? 60 : 1;

            return (int)Math.Ceiling(upperBoundValue * minutesPerUnit);
        }
    }
}