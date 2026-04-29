using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using CulinaryCommandSmartTaskMcp.Models;
using CulinaryCommandSmartTaskMcp.Services;
using Google.GenAI;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CulinaryCommandSmartTaskMcp
{
    public sealed class Function
    {
        private const string GeminiApiKeySsmParameterName = "/culinarycommand/prod/SmartTask/GeminiApiKey";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly SmartTaskPlanner _planner;

        public Function()
        {
            var heuristics = new HeuristicFallback();
            var serviceWindowClock = new ServiceWindowClock();

            // Best-effort cold-start load of the Gemini key. If anything fails (no SSM
            // permissions, no env var, package missing) the planner just runs the
            // heuristic-only path with no Gemini enrichment — never throws on cold start.
            GeminiPlanner? geminiPlanner = null;
            try
            {
                var geminiApiKey = LoadGeminiApiKey();
                if (!string.IsNullOrWhiteSpace(geminiApiKey))
                {
                    var geminiClient = new Client(apiKey: geminiApiKey);
                    geminiPlanner = new GeminiPlanner(geminiClient);
                }
            }
            catch (Exception failure)
            {
                Console.WriteLine($"[SmartTask] Gemini disabled at cold start: {failure.Message}");
            }

            _planner = new SmartTaskPlanner(heuristics, serviceWindowClock, geminiPlanner);
        }

        public async Task<APIGatewayHttpApiV2ProxyResponse> Handle(
            APIGatewayHttpApiV2ProxyRequest httpRequest,
            ILambdaContext lambdaContext)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(httpRequest.Body))
                    return BadRequest("Request body is required.");

                var planRequest = JsonSerializer.Deserialize<PlanRequest>(httpRequest.Body, JsonOptions)
                    ?? throw new InvalidOperationException("Could not deserialize PlanRequest.");

                var planResponse = await _planner.PlanAsync(planRequest);

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                    Body = JsonSerializer.Serialize(planResponse, JsonOptions)
                };
            }
            catch (Exception failure)
            {
                lambdaContext.Logger.LogError($"SmartTask planning failed: {failure}");
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 500,
                    Body = JsonSerializer.Serialize(new { error = failure.Message })
                };
            }
        }

        private static string? LoadGeminiApiKey()
        {
            // Local invocation / dev convenience: respect an env var if set.
            var envKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey)) return envKey;

            try
            {
                using var ssmClient = new AmazonSimpleSystemsManagementClient();
                var ssmResponse = ssmClient.GetParameterAsync(new GetParameterRequest
                {
                    Name = GeminiApiKeySsmParameterName,
                    WithDecryption = true
                }).GetAwaiter().GetResult();
                return ssmResponse?.Parameter?.Value;
            }
            catch
            {
                return null;
            }
        }

        private static APIGatewayHttpApiV2ProxyResponse BadRequest(string message) => new()
        {
            StatusCode = 400,
            Body = JsonSerializer.Serialize(new { error = message })
        };
    }
}
