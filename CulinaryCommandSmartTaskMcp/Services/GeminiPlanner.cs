using System.Text.Json;
using CulinaryCommandSmartTaskMcp.Models;
using Google.GenAI;

namespace CulinaryCommandSmartTaskMcp.Services
{
    /// Optional Gemini-backed classifier used as a tie-breaker when the heuristic
    /// fallback returns "AllDay" but the title is suggestive of a real meal.
    public sealed class GeminiPlanner
    {
        private const string ServiceWindowPrompt =
            "Return ONLY a JSON object with this exact shape: " +
            "{\"serviceWindow\":\"Breakfast|Brunch|Lunch|Dinner|LateNight|AllDay\",\"confidence\":0.0-1.0}. " +
            "When is this restaurant recipe normally served?";

        private static readonly HashSet<string> ValidServiceWindows = new(StringComparer.OrdinalIgnoreCase)
        {
            "Breakfast", "Brunch", "Lunch", "Dinner", "LateNight", "AllDay"
        };

        private readonly Client? _geminiClient;

        public GeminiPlanner(Client? geminiClient)
        {
            _geminiClient = geminiClient;
        }

        public bool IsAvailable => _geminiClient is not null;

        public async Task<GeminiClassification?> ClassifyServiceWindowAsync(RecipeInput recipe)
        {
            if (_geminiClient is null) return null;

            var prompt = $"{ServiceWindowPrompt}\nTitle: {recipe.Title}\nCategory: {recipe.Category}\n" +
                         $"RecipeType: {recipe.RecipeType}";

            try
            {
                var geminiResponse = await _geminiClient.Models.GenerateContentAsync(
                    model: "gemini-2.0-flash",
                    contents: prompt);

                var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrWhiteSpace(rawJson)) return null;

                // Some models wrap JSON in ```json fences — strip them defensively.
                rawJson = rawJson.Trim();
                if (rawJson.StartsWith("```"))
                {
                    var firstNewline = rawJson.IndexOf('\n');
                    var lastFence = rawJson.LastIndexOf("```", StringComparison.Ordinal);
                    if (firstNewline > 0 && lastFence > firstNewline)
                        rawJson = rawJson.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
                }

                using var parsedDocument = JsonDocument.Parse(rawJson);
                var root = parsedDocument.RootElement;

                if (!root.TryGetProperty("serviceWindow", out var windowEl)) return null;
                var window = windowEl.GetString();
                if (string.IsNullOrWhiteSpace(window) || !ValidServiceWindows.Contains(window)) return null;

                double confidence = 0.5;
                if (root.TryGetProperty("confidence", out var confEl) && confEl.ValueKind == JsonValueKind.Number)
                {
                    confidence = confEl.GetDouble();
                }

                return new GeminiClassification(window, confidence);
            }
            catch
            {
                // Gemini is best-effort; never let a network/parse error sink the plan.
                return null;
            }
        }
    }

    public sealed record GeminiClassification(string ServiceWindow, double Confidence);
}
