using CulinaryCommandSmartTaskMcp.Models;

namespace CulinaryCommandSmartTaskMcp.Services
{
    public sealed class SmartTaskPlanner
    {
        private const double GeminiConfidenceThreshold = 0.7;

        private readonly HeuristicFallback _heuristics;
        private readonly ServiceWindowClock _serviceWindowClock;
        private readonly GeminiPlanner? _geminiPlanner;

        public SmartTaskPlanner(
            HeuristicFallback heuristics,
            ServiceWindowClock serviceWindowClock,
            GeminiPlanner? geminiPlanner = null)
        {
            _heuristics = heuristics;
            _serviceWindowClock = serviceWindowClock;
            _geminiPlanner = geminiPlanner;
        }

        public PlanResponse Plan(PlanRequest request) =>
            PlanAsync(request).GetAwaiter().GetResult();

        public async Task<PlanResponse> PlanAsync(PlanRequest request)
        {
            var openTaskCountByUser = request.EligibleUsers
                .ToDictionary(user => user.UserId, user => user.OpenTaskCountToday);

            var plannedTasks = new List<PlannedPrepTask>();

            foreach (var recipe in request.Recipes)
            {
                var serviceWindow = _heuristics.ClassifyServiceWindow(recipe);
                var classifierUsed = "heuristic";

                // If the heuristic punted to "AllDay" and Gemini is available, ask Gemini.
                if (string.Equals(serviceWindow, "AllDay", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(recipe.ServiceWindow) &&
                    _geminiPlanner is { IsAvailable: true })
                {
                    var geminiResult = await _geminiPlanner.ClassifyServiceWindowAsync(recipe);
                    if (geminiResult is not null && geminiResult.Confidence >= GeminiConfidenceThreshold)
                    {
                        serviceWindow = geminiResult.ServiceWindow;
                        classifierUsed = $"gemini ({geminiResult.Confidence:0.00})";
                    }
                }

                var leadTimeMinutes = _heuristics.EstimatePrepLeadTimeMinutes(recipe, request.Defaults);
                var serviceStartUtc = _serviceWindowClock.ResolveServiceStartUtc(
                    request.ServiceDate, serviceWindow, recipe.ServiceTimeOverride);
                var dueDateUtc = serviceStartUtc.AddMinutes(-leadTimeMinutes);
                var assignedUserId = PickLeastLoadedUser(openTaskCountByUser);
                var priority = _heuristics.SuggestPriority(leadTimeMinutes, recipe.SubRecipes.Count);

                openTaskCountByUser[assignedUserId] += 1;

                plannedTasks.Add(new PlannedPrepTask(
                    RecipeId: recipe.RecipeId,
                    RecipeTitle: recipe.Title,
                    AssignedUserId: assignedUserId,
                    DueDateUtc: dueDateUtc,
                    LeadTimeMinutes: leadTimeMinutes,
                    Priority: priority,
                    ServiceWindow: serviceWindow,
                    ReasoningSummary: $"{serviceWindow} service @ {serviceStartUtc:HH:mm}, " +
                                      $"{leadTimeMinutes}m lead time (classifier: {classifierUsed})."));
            }

            return new PlanResponse(plannedTasks);
        }

        private static int PickLeastLoadedUser(Dictionary<int, int> openTaskCountByUser)
        {
            if (openTaskCountByUser.Count == 0)
                throw new InvalidOperationException("No eligible users were supplied for the location.");

            return openTaskCountByUser
                .OrderBy(entry => entry.Value)
                .ThenBy(entry => entry.Key)
                .First().Key;
        }
    }
}