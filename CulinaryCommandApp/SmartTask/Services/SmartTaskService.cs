using System.Collections.Concurrent;
using System.Text.Json;
using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommand.Data.Enums;
using CulinaryCommand.Services;
using CulinaryCommandApp.SmartTask.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using WorkTaskStatus = CulinaryCommand.Data.Enums.TaskStatus;

namespace CulinaryCommandApp.SmartTask.Services
{
    public sealed class SmartTaskService : ISmartTaskService
    {
        private static readonly ConcurrentDictionary<Guid, SmartTaskPlanPreview> PlanPreviewCache = new();

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ISmartTaskOrchestratorClient _orchestratorClient;
        private readonly ITaskNotificationService _taskNotificationService;
        private readonly IConfiguration _configuration;

        public SmartTaskService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ISmartTaskOrchestratorClient orchestratorClient,
            ITaskNotificationService taskNotificationService,
            IConfiguration configuration)
        {
            _dbContextFactory = dbContextFactory;
            _orchestratorClient = orchestratorClient;
            _taskNotificationService = taskNotificationService;
            _configuration = configuration;
        }

        public async Task<SmartTaskPlanPreview> PreviewAsync(
            SmartTaskRequest request, CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var requestedRecipes = await dbContext.Recipes
                .AsNoTracking()
                .Where(r => r.LocationId == request.LocationId && request.RecipeIds.Contains(r.RecipeId))
                .Include(r => r.Steps)
                .ToListAsync(cancellationToken);

            var eligibleUsers = await BuildEligibleUserPoolAsync(
                dbContext, request.LocationId, request.ServiceDate, cancellationToken);

            var planRequestDto = new PlanRequestDto(
                LocationId: request.LocationId,
                ServiceDate: request.ServiceDate,
                Recipes: requestedRecipes.Select(MapRecipeToDto).ToList(),
                EligibleUsers: eligibleUsers,
                Defaults: new PlanDefaultsDto(
                    DefaultPrepBufferMinutes: _configuration.GetValue("SmartTask:DefaultPrepBufferMinutes", 30),
                    DefaultLeadTimeWhenUnknown: _configuration.GetValue("SmartTask:DefaultLeadTimeWhenUnknown", 60)));

            var plan = await _orchestratorClient.RequestPlanAsync(planRequestDto, cancellationToken);

            var preview = new SmartTaskPlanPreview
            {
                OriginalRequest = request,
                Plan = plan
            };
            PlanPreviewCache[preview.PreviewId] = preview;
            return preview;
        }

        public async Task<SmartTaskRun> CommitAsync(Guid planPreviewId, CancellationToken cancellationToken)
        {
            if (!PlanPreviewCache.TryRemove(planPreviewId, out var preview))
                throw new InvalidOperationException("Plan preview expired or not found. Regenerate the plan.");

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // EnableRetryOnFailure requires the entire transactional unit to run inside
            // the execution strategy so it can retry the whole block on transient errors.
            var executionStrategy = dbContext.Database.CreateExecutionStrategy();

            var (smartTaskRun, createdTasks) = await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                var run = new SmartTaskRun
                {
                    LocationId = preview.OriginalRequest.LocationId,
                    TriggeredByUserId = preview.OriginalRequest.TriggeredByUserId,
                    ServiceDate = preview.OriginalRequest.ServiceDate,
                    RecipeIdsJson = JsonSerializer.Serialize(preview.OriginalRequest.RecipeIds),
                    CreatedTaskIdsJson = "[]",
                    Status = nameof(SmartTaskRunStatus.Planned)
                };
                dbContext.SmartTaskRuns.Add(run);
                await dbContext.SaveChangesAsync(cancellationToken);

                var tasks = preview.Plan.PlannedTasks
                    .Select(plannedTask => BuildTaskFromPlan(plannedTask, run, preview.OriginalRequest))
                    .ToList();

                dbContext.Tasks.AddRange(tasks);
                await dbContext.SaveChangesAsync(cancellationToken);

                run.CreatedTaskIdsJson = JsonSerializer.Serialize(tasks.Select(t => t.Id));
                run.Status = nameof(SmartTaskRunStatus.Committed);
                run.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return (run, tasks);
            });

            foreach (var createdTask in createdTasks)
            {
                _taskNotificationService.NotifyTaskAssigned(createdTask);
            }
            return smartTaskRun;
        }

        public async Task RollbackAsync(Guid runId, CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var smartTaskRun = await dbContext.SmartTaskRuns
                .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
                ?? throw new InvalidOperationException("SmartTask run not found.");

            if (smartTaskRun.Status == nameof(SmartTaskRunStatus.RolledBack))
                return;

            var createdTaskIds = JsonSerializer.Deserialize<List<int>>(smartTaskRun.CreatedTaskIdsJson)
                ?? new List<int>();

            var tasksFromRun = await dbContext.Tasks
                .Where(t => createdTaskIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            var anyTaskAlreadyStarted = tasksFromRun.Any(t =>
                t.Status == WorkTaskStatus.InProgress || t.Status == WorkTaskStatus.Completed);

            if (anyTaskAlreadyStarted)
                throw new InvalidOperationException(
                    "One or more tasks are already in progress or completed; rollback is blocked.");

            dbContext.Tasks.RemoveRange(tasksFromRun);
            smartTaskRun.Status = nameof(SmartTaskRunStatus.RolledBack);
            smartTaskRun.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SmartTaskRun>> RecentRunsAsync(int locationId, int take = 10)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await dbContext.SmartTaskRuns
                .AsNoTracking()
                .Where(r => r.LocationId == locationId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        private static async Task<List<EligibleUserDto>> BuildEligibleUserPoolAsync(
            AppDbContext dbContext, int locationId, DateOnly serviceDate, CancellationToken cancellationToken)
        {
            var serviceDayOfWeek = serviceDate.DayOfWeek;
            var serviceDayStartUtc = new DateTime(serviceDate, TimeOnly.MinValue, DateTimeKind.Utc);
            var serviceDayEndUtc = serviceDayStartUtc.AddDays(1);

            // Employees assigned via UserLocations.
            var employeesAtLocation = await dbContext.UserLocations
                .AsNoTracking()
                .Where(ul => ul.LocationId == locationId
                          && ul.User != null
                          && ul.User.IsActive
                          && ul.User.Role != Roles.Admin)
                .Select(ul => ul.User!)
                .ToListAsync(cancellationToken);

            // Managers assigned via the separate ManagerLocations join table.
            var managersAtLocation = await dbContext.ManagerLocations
                .AsNoTracking()
                .Where(ml => ml.LocationId == locationId
                          && ml.User != null
                          && ml.User.IsActive
                          && ml.User.Role != Roles.Admin)
                .Select(ml => ml.User!)
                .ToListAsync(cancellationToken);

            var eligibleUsersAtLocation = employeesAtLocation
                .Concat(managersAtLocation)
                .GroupBy(user => user.Id)
                .Select(group => group.First())
                .ToList();

            if (eligibleUsersAtLocation.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No eligible users (Employees or Managers) are assigned to location {locationId}. " +
                    "Assign at least one non-admin user to this location before generating a plan.");
            }

            // Availability filter: prefer users with an availability row for the service day-of-week.
            // If nobody has availability set for that day, fall back to the full eligible pool.
            var availableUserIdsForServiceDay = await dbContext.UserAvailabilities
                .AsNoTracking()
                .Where(availability => availability.LocationId == locationId
                                    && availability.DayOfWeek == serviceDayOfWeek)
                .Select(availability => availability.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var availableUserIdSet = availableUserIdsForServiceDay.ToHashSet();

            var usersWithAvailabilityOnServiceDay = eligibleUsersAtLocation
                .Where(user => availableUserIdSet.Contains(user.Id))
                .ToList();

            var usersToAssign = usersWithAvailabilityOnServiceDay.Count > 0
                ? usersWithAvailabilityOnServiceDay
                : eligibleUsersAtLocation;

            var openTaskCountByUser = await dbContext.Tasks
                .AsNoTracking()
                .Where(t => t.LocationId == locationId
                         && t.UserId != null
                         && t.DueDate >= serviceDayStartUtc
                         && t.DueDate < serviceDayEndUtc)
                .GroupBy(t => t.UserId!.Value)
                .Select(g => new { UserId = g.Key, OpenCount = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.OpenCount, cancellationToken);

            return usersToAssign
                .Select(user => new EligibleUserDto(
                    UserId: user.Id,
                    DisplayName: user.Name ?? user.Email ?? $"User {user.Id}",
                    OpenTaskCountToday: openTaskCountByUser.GetValueOrDefault(user.Id, 0)))
                .ToList();
        }

        private static RecipeInputDto MapRecipeToDto(CulinaryCommandApp.Recipe.Entities.Recipe recipe) =>
            new(
                RecipeId: recipe.RecipeId,
                Title: recipe.Title,
                Category: recipe.Category,
                RecipeType: recipe.RecipeType,
                ServiceWindow: recipe.ServiceWindow?.ToString(),
                ServiceTimeOverride: recipe.ServiceTimeOverride,
                PrepLeadTimeMinutesOverride: recipe.PrepLeadTimeMinutes,
                Steps: recipe.Steps
                    .OrderBy(step => step.StepNumber)
                    .Select(step => new RecipeStepInputDto(step.StepNumber, step.Duration))
                    .ToList(),
                SubRecipes: Array.Empty<RecipeInputDto>());

        private static Tasks BuildTaskFromPlan(
            PlannedPrepTaskDto plannedTask, SmartTaskRun owningRun, SmartTaskRequest originalRequest) =>
            new()
            {
                Name = plannedTask.RecipeTitle,
                Station = Station.Prep,
                Status = WorkTaskStatus.Pending,
                Assigner = "SmartTask",
                Date = DateTime.UtcNow,
                UserId = plannedTask.AssignedUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = plannedTask.DueDateUtc,
                LocationId = originalRequest.LocationId,
                Kind = WorkTaskKind.PrepFromRecipe,
                RecipeId = plannedTask.RecipeId,
                Priority = plannedTask.Priority,
                Notes = plannedTask.ReasoningSummary,
                SmartTaskRunId = owningRun.Id,
                GeneratedBy = "SmartTask"
            };
    }
}