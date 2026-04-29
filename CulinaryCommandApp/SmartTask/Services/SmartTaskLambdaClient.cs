using System.Net.Http.Json;
using System.Text.Json;
using CulinaryCommandApp.SmartTask.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CulinaryCommandApp.SmartTask.Services
{
    public sealed class SmartTaskLambdaClient : ISmartTaskOrchestratorClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            // Outbound payload uses camelCase; the Lambda deserializes case-insensitively.
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Inbound payload from the Lambda is PascalCase (no naming policy on its side),
            // so we must accept either casing on deserialize.
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<SmartTaskLambdaClient> _logger;

        public SmartTaskLambdaClient(HttpClient httpClient, ILogger<SmartTaskLambdaClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PlanResponseDto> RequestPlanAsync(
            PlanRequestDto planRequest, CancellationToken cancellationToken)
        {
            try
            {
                var lambdaResponse = await _httpClient.PostAsJsonAsync(
                    string.Empty, planRequest, JsonOptions, cancellationToken);

                if (!lambdaResponse.IsSuccessStatusCode)
                {
                    var responseBody = await lambdaResponse.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogError(
                        "SmartTask Function URL call failed. Status={StatusCode}. Body={Body}",
                        (int)lambdaResponse.StatusCode,
                        responseBody);

                    // Surface details to callers (still caught below and wrapped).
                    throw new HttpRequestException(
                        $"SmartTask Function URL returned {(int)lambdaResponse.StatusCode} ({lambdaResponse.StatusCode}). Body: {responseBody}");
                }

                var deserializedPlan = await lambdaResponse.Content.ReadFromJsonAsync<PlanResponseDto>(
                    JsonOptions, cancellationToken);

                return deserializedPlan
                    ?? throw new InvalidOperationException("Lambda returned an empty plan body.");
            }
            catch (Exception failure) when (failure is HttpRequestException or TaskCanceledException)
            {
                throw new SmartTaskOrchestratorUnavailableException(
                    "SmartTask planner is currently unavailable.", failure);
            }
        }
    }
}