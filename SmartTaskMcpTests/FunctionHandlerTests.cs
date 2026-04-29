using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using CulinaryCommandSmartTaskMcp.Models;
using Xunit;

namespace CulinaryCommandSmartTaskMcp.Tests
{
    public class FunctionHandlerTests
    {
        private readonly Function _function = new();

        [Fact]
        public async Task ReturnsPlannedTasksForValidRequest()
        {
            var planRequest = new PlanRequest(
                LocationId: 1,
                ServiceDate: new DateOnly(2026, 5, 1),
                Recipes: new[]
                {
                    new RecipeInput(1, "Eggs Benedict", "Brunch", "Entree",
                        null, null, null,
                        Array.Empty<RecipeStepInput>(),
                        Array.Empty<RecipeInput>())
                },
                EligibleUsers: new[] { new EligibleUser(10, "Alice", 0) },
                Defaults: new PlanDefaults(30, 60));

            var httpRequest = new APIGatewayHttpApiV2ProxyRequest
            {
                Body = JsonSerializer.Serialize(planRequest)
            };

            var response = await _function.Handle(httpRequest, new TestLambdaContext());

            Assert.Equal(200, response.StatusCode);
            var planResponse = JsonSerializer.Deserialize<PlanResponse>(response.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(planResponse);
            Assert.Single(planResponse.PlannedTasks);
            Assert.Equal(10, planResponse.PlannedTasks[0].AssignedUserId);
        }

        [Fact]
        public async Task Returns400WhenBodyIsEmpty()
        {
            var httpRequest = new APIGatewayHttpApiV2ProxyRequest { Body = null };
            var response = await _function.Handle(httpRequest, new TestLambdaContext());
            Assert.Equal(400, response.StatusCode);
        }
    }
}