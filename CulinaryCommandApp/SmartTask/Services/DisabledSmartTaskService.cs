using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Data.Entities;
using CulinaryCommandApp.SmartTask.Services.Interfaces;

namespace CulinaryCommandApp.SmartTask.Services
{
    /// <summary>
    /// Local/dev fallback when SmartTask is disabled via configuration.
    /// </summary>
    public sealed class DisabledSmartTaskService : ISmartTaskService
    {
        public Task<SmartTaskPlanPreview> PreviewAsync(SmartTaskRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("SmartTask is disabled. Set SmartTask:Enabled=true and configure SmartTask:LambdaFunctionUrlEndpoint to use it.");

        public Task<SmartTaskRun> CommitAsync(Guid planPreviewId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("SmartTask is disabled. Set SmartTask:Enabled=true and configure SmartTask:LambdaFunctionUrlEndpoint to use it.");

        public Task RollbackAsync(Guid runId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("SmartTask is disabled. Set SmartTask:Enabled=true and configure SmartTask:LambdaFunctionUrlEndpoint to use it.");

        public Task<List<SmartTaskRun>> RecentRunsAsync(int locationId, int take = 10)
            => Task.FromResult(new List<SmartTaskRun>());
    }
}
