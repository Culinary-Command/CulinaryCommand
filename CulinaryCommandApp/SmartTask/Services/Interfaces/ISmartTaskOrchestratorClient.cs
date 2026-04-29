namespace CulinaryCommandApp.SmartTask.Services.Interfaces
{
    public interface ISmartTaskOrchestratorClient
    {
        Task<PlanResponseDto> RequestPlanAsync(PlanRequestDto planRequest, CancellationToken cancellationToken);
    }

    public sealed class SmartTaskOrchestratorUnavailableException : Exception
    {
        public SmartTaskOrchestratorUnavailableException(string message, Exception inner)
            : base(message, inner) { }
    }


}
    
