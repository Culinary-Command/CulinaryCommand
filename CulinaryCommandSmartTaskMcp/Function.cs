using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using CulinaryCommandSmartTaskMcp.Models;
using CulinaryCommandSmartTaskMcp.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CulinaryCommandSmartTaskMcp
{
    public sealed class Function
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly SmartTaskPlanner _planner;

        public Function()
        {
            var heuristics = new HeuristicFallback();
            var serviceWindowClock = new ServiceWindowClock();
            _planner = new SmartTaskPlanner(heuristics, serviceWindowClock);
        }

        public APIGatewayHttpApiV2ProxyResponse Handle(APIGatewayHttpApiV2ProxyRequest httpRequest, ILambdaContext lambdaContext)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(httpRequest.Body))
                    return BadRequest("Request body is required.");

                var planRequest = JsonSerializer.Deserialize<PlanRequest>(httpRequest.Body, JsonOptions)
                    ?? throw new InvalidOperationException("Could not deserialize PlanRequest.");

                var planResponse = _planner.Plan(planRequest);

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

        private static APIGatewayHttpApiV2ProxyResponse BadRequest(string message) => new()
        {
            StatusCode = 400,
            Body = JsonSerializer.Serialize(new { error = message })
        };
    }
}