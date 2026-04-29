using System.ComponentModel;
using System.Text.Json;
using CulinaryCommandSmartTaskMcp.Models;
using CulinaryCommandSmartTaskMcp.Services;
using ModelContextProtocol.Server;

namespace CulinaryCommandSmartTaskMcp.Tools
{
    /// MCP tool surface for SmartTask planning. Each method below is callable
    /// individually so an outer agent can orchestrate, OR `generate_plan` can
    /// be called once for the full pipeline.
    [McpServerToolType]
    public sealed class SmartTaskTools
    {
        private readonly HeuristicFallback _heuristics;
        private readonly ServiceWindowClock _serviceWindowClock;
        private readonly SmartTaskPlanner _planner;
        private readonly GeminiPlanner? _gemini;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public SmartTaskTools(
            HeuristicFallback heuristics,
            ServiceWindowClock serviceWindowClock,
            SmartTaskPlanner planner,
            GeminiPlanner? gemini = null)
        {
            _heuristics = heuristics;
            _serviceWindowClock = serviceWindowClock;
            _planner = planner;
            _gemini = gemini;
        }

        // -----------------------------------------------------------------
        // Tool 1: classify_service_window
        // -----------------------------------------------------------------
        [McpServerTool(Name = "classify_service_window")]
        [Description(
            "Decides which service window (Breakfast, Brunch, Lunch, Dinner, " +
            "LateNight, AllDay) a recipe is normally served in. Uses fast " +
            "regex heuristics first; if the heuristic returns AllDay and a " +
            "Gemini key is configured on the server, asks Gemini as a tiebreaker.")]
        public async Task<string> ClassifyServiceWindowAsync(
            [Description("Recipe display title, e.g. \"Eggs Benedict\".")] string title,
            [Description("Recipe category, e.g. \"Brunch\" or \"Side\".")] string category,
            [Description("Recipe type, e.g. \"Entree\", \"Dessert\".")] string recipeType,
            [Description("Optional manager-provided override; if set, returned as-is.")] string? overrideWindow = null)
        {
            var recipe = new RecipeInput(
                RecipeId: 0,
                Title: title,
                Category: category,
                RecipeType: recipeType,
                ServiceWindow: overrideWindow,
                ServiceTimeOverride: null,
                PrepLeadTimeMinutesOverride: null,
                Steps: Array.Empty<RecipeStepInput>(),
                SubRecipes: Array.Empty<RecipeInput>());

            var heuristicResult = _heuristics.ClassifyServiceWindow(recipe);

            // Only escalate to Gemini for ambiguous cases.
            if (string.Equals(heuristicResult, "AllDay", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(overrideWindow) &&
                _gemini is { IsAvailable: true })
            {
                var geminiClassification = await _gemini.ClassifyServiceWindowAsync(recipe);
                if (geminiClassification is not null && geminiClassification.Confidence >= 0.7)
                {
                    return JsonSerializer.Serialize(new
                    {
                        serviceWindow = geminiClassification.ServiceWindow,
                        classifier = "gemini",
                        confidence = geminiClassification.Confidence
                    }, JsonOptions);
                }
            }

            return JsonSerializer.Serialize(new
            {
                serviceWindow = heuristicResult,
                classifier = "heuristic",
                confidence = 1.0
            }, JsonOptions);
        }

        // -----------------------------------------------------------------
        // Tool 2: estimate_prep_lead_time
        // -----------------------------------------------------------------
        [McpServerTool(Name = "estimate_prep_lead_time")]
        [Description(
            "Estimates how many minutes before service a prep task should start. " +
            "Sums parsed step durations (\"15 min\", \"1.5 hr\", \"8-10 minutes\") " +
            "and sub-recipe lead times, then adds the configured buffer. If the " +
            "recipe has no parseable durations, returns the configured default.")]
        public int EstimatePrepLeadTimeMinutes(
            [Description("Recipe step durations, in order, as free-form strings (e.g. \"15 minutes\").")] string[] stepDurations,
            [Description("Pre-computed lead times of any sub-recipes, in minutes.")] int[]? subRecipeLeadTimes = null,
            [Description("Optional manager override; if set, returned as-is and short-circuits the estimate.")] int? prepLeadTimeMinutesOverride = null,
            [Description("Buffer added to the parsed total. Defaults to 30.")] int defaultPrepBufferMinutes = 30,
            [Description("Lead time used when no durations are parseable. Defaults to 60.")] int defaultLeadTimeWhenUnknown = 60)
        {
            var recipe = new RecipeInput(
                RecipeId: 0,
                Title: string.Empty,
                Category: string.Empty,
                RecipeType: string.Empty,
                ServiceWindow: null,
                ServiceTimeOverride: null,
                PrepLeadTimeMinutesOverride: prepLeadTimeMinutesOverride,
                Steps: stepDurations
                    .Select((duration, index) => new RecipeStepInput(index + 1, duration))
                    .ToList(),
                SubRecipes: (subRecipeLeadTimes ?? Array.Empty<int>())
                    .Select(_ => new RecipeInput(
                        0, string.Empty, string.Empty, string.Empty,
                        null, null, null,
                        Array.Empty<RecipeStepInput>(),
                        Array.Empty<RecipeInput>()))
                    .ToList());

            var defaults = new PlanDefaults(defaultPrepBufferMinutes, defaultLeadTimeWhenUnknown);
            var heuristicLeadTime = _heuristics.EstimatePrepLeadTimeMinutes(recipe, defaults);

            // Add precomputed sub-recipe lead times manually since the heuristic
            // only recurses into nested RecipeInput objects, not bare totals.
            var subRecipeTotal = (subRecipeLeadTimes ?? Array.Empty<int>()).Sum();
            return heuristicLeadTime + subRecipeTotal;
        }

        // -----------------------------------------------------------------
        // Tool 3: pick_least_loaded_user
        // -----------------------------------------------------------------
        [McpServerTool(Name = "pick_least_loaded_user")]
        [Description(
            "Given a set of eligible users with their current open task counts, " +
            "returns the user id that should receive the next prep task. Ties " +
            "break by lowest user id for determinism.")]
        public int PickLeastLoadedUser(
            [Description("Eligible user ids in the order they were supplied to the planner.")] int[] userIds,
            [Description("Open task count today for each user, parallel array to userIds.")] int[] openTaskCounts)
        {
            if (userIds.Length == 0)
                throw new InvalidOperationException("No eligible users supplied.");
            if (userIds.Length != openTaskCounts.Length)
                throw new InvalidOperationException("userIds and openTaskCounts must be the same length.");

            var byUser = userIds.Zip(openTaskCounts, (id, count) => (id, count))
                .OrderBy(t => t.count)
                .ThenBy(t => t.id)
                .ToList();

            return byUser[0].id;
        }

        // -----------------------------------------------------------------
        // Tool 4: resolve_service_start
        // -----------------------------------------------------------------
        [McpServerTool(Name = "resolve_service_start")]
        [Description(
            "Maps a service window name and date to the absolute UTC datetime when " +
            "service starts. If a service-time override is provided, it wins.")]
        public string ResolveServiceStartUtc(
            [Description("ISO date, e.g. \"2026-05-01\".")] string serviceDateIso,
            [Description("Service window name (Breakfast, Brunch, Lunch, Dinner, LateNight, AllDay).")] string serviceWindow,
            [Description("Optional service-time override in HH:mm 24h format.")] string? serviceTimeOverride = null)
        {
            if (!DateOnly.TryParse(serviceDateIso, out var serviceDate))
                throw new ArgumentException($"serviceDateIso '{serviceDateIso}' is not a valid date.");

            TimeOnly? overrideTime = null;
            if (!string.IsNullOrWhiteSpace(serviceTimeOverride))
            {
                if (!TimeOnly.TryParse(serviceTimeOverride, out var parsed))
                    throw new ArgumentException($"serviceTimeOverride '{serviceTimeOverride}' is not a valid time (HH:mm).");
                overrideTime = parsed;
            }

            var serviceStartUtc = _serviceWindowClock.ResolveServiceStartUtc(serviceDate, serviceWindow, overrideTime);
            return serviceStartUtc.ToString("O");
        }

        // -----------------------------------------------------------------
        // Tool 5: generate_plan — the full pipeline
        // -----------------------------------------------------------------
        [McpServerTool(Name = "generate_plan")]
        [Description(
            "Runs the entire SmartTask planning pipeline in one call: classifies " +
            "each recipe's service window, estimates lead time, resolves service " +
            "start, and load-balances assignments across eligible users. Accepts " +
            "the same JSON shape the deployed Lambda accepts. Returns the full " +
            "plan as JSON.")]
        public async Task<string> GeneratePlanAsync(
            [Description("PlanRequest JSON: { locationId, serviceDate, recipes[], eligibleUsers[], defaults }.")] string planRequestJson)
        {
            var planRequest = JsonSerializer.Deserialize<PlanRequest>(planRequestJson, JsonOptions)
                ?? throw new ArgumentException("planRequestJson did not deserialize into a PlanRequest.");

            var planResponse = await _planner.PlanAsync(planRequest);
            return JsonSerializer.Serialize(planResponse, JsonOptions);
        }
    }
}
