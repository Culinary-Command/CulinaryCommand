using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommand.Data.Enums;
using CulinaryCommand.Services;
using CulinaryCommandApp.SmartTask.Services;
using CulinaryCommandApp.SmartTask.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using DomainTaskStatus = CulinaryCommand.Data.Enums.TaskStatus;

namespace CulinaryCommand.Tests.SmartTask
{
    public class SmartTaskServiceTests
    {
        [Fact]
        public async Task CommitWritesTasksAndStampsRunId()
        {
            var ct = global::Xunit.TestContext.Current.CancellationToken;

            using var dbContext = BuildSeededDbContext(out var dbOptions);
            var dbContextFactory = BuildDbContextFactory(dbOptions);

            var fakeOrchestratorClient = new Mock<ISmartTaskOrchestratorClient>();
            fakeOrchestratorClient
                .Setup(client => client.RequestPlanAsync(It.IsAny<PlanRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlanResponseDto(new List<PlannedPrepTaskDto>
                {
                    new(1, "Eggs Benedict", 99, DateTime.UtcNow.AddHours(6), 45, "Normal", "Breakfast", "ok")
                }));

            var notifier = new Mock<ITaskNotificationService>();
            var configuration = new ConfigurationBuilder().Build();

            var smartTaskService = new SmartTaskService(
                dbContextFactory, fakeOrchestratorClient.Object, notifier.Object, configuration);

            var preview = await smartTaskService.PreviewAsync(
                new SmartTaskRequest(1, new DateOnly(2026, 5, 1), new[] { 1 }, 9),
                ct);

            var committedRun = await smartTaskService.CommitAsync(preview.PreviewId, ct);

            var persistedTask = await dbContext.Tasks.SingleAsync(ct);
            Assert.Equal(committedRun.Id, persistedTask.SmartTaskRunId);
            Assert.Equal("SmartTask", persistedTask.GeneratedBy);
            notifier.Verify(n => n.NotifyTaskAssigned(It.IsAny<Tasks>()), Times.Once);
        }

        [Fact]
        public async Task RollbackDeletesPendingTasksAndMarksRunRolledBack()
        {
            var ct = global::Xunit.TestContext.Current.CancellationToken;

            using var dbContext = BuildSeededDbContext(out var dbOptions);
            var dbContextFactory = BuildDbContextFactory(dbOptions);

            var fakeOrchestratorClient = new Mock<ISmartTaskOrchestratorClient>();
            fakeOrchestratorClient
                .Setup(client => client.RequestPlanAsync(It.IsAny<PlanRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlanResponseDto(new List<PlannedPrepTaskDto>
                {
                    new(1, "Eggs Benedict", 99, DateTime.UtcNow.AddHours(6), 45, "Normal", "Breakfast", "ok")
                }));

            var smartTaskService = new SmartTaskService(
                dbContextFactory, fakeOrchestratorClient.Object,
                new Mock<ITaskNotificationService>().Object,
                new ConfigurationBuilder().Build());

            var preview = await smartTaskService.PreviewAsync(
                new SmartTaskRequest(1, new DateOnly(2026, 5, 1), new[] { 1 }, 9),
                ct);
            var committedRun = await smartTaskService.CommitAsync(preview.PreviewId, ct);

            await smartTaskService.RollbackAsync(committedRun.Id, ct);

            Assert.Empty(dbContext.Tasks);
            var rolledBackRun = await dbContext.SmartTaskRuns.SingleAsync(ct);
            Assert.Equal(nameof(SmartTaskRunStatus.RolledBack), rolledBackRun.Status);
        }

        [Fact]
        public async Task RollbackThrowsWhenAnyTaskIsInProgress()
        {
            var ct = global::Xunit.TestContext.Current.CancellationToken;

            using var dbContext = BuildSeededDbContext(out var dbOptions);
            var dbContextFactory = BuildDbContextFactory(dbOptions);

            var fakeOrchestratorClient = new Mock<ISmartTaskOrchestratorClient>();
            fakeOrchestratorClient
                .Setup(client => client.RequestPlanAsync(It.IsAny<PlanRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlanResponseDto(new List<PlannedPrepTaskDto>
                {
                    new(1, "Eggs Benedict", 99, DateTime.UtcNow.AddHours(6), 45, "Normal", "Breakfast", "ok")
                }));

            var smartTaskService = new SmartTaskService(
                dbContextFactory, fakeOrchestratorClient.Object,
                new Mock<ITaskNotificationService>().Object,
                new ConfigurationBuilder().Build());

            var preview = await smartTaskService.PreviewAsync(
                new SmartTaskRequest(1, new DateOnly(2026, 5, 1), new[] { 1 }, 9),
                ct);
            var committedRun = await smartTaskService.CommitAsync(preview.PreviewId, ct);

            var createdTask = await dbContext.Tasks.SingleAsync(ct);
            createdTask.Status = DomainTaskStatus.InProgress;
            await dbContext.SaveChangesAsync(ct);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => smartTaskService.RollbackAsync(committedRun.Id, ct));
        }

        private static IDbContextFactory<AppDbContext> BuildDbContextFactory(DbContextOptions<AppDbContext> dbOptions)
        {
            var factory = new Mock<IDbContextFactory<AppDbContext>>();
            factory
                .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new AppDbContext(dbOptions));
            return factory.Object;
        }

        private static AppDbContext BuildSeededDbContext(out DbContextOptions<AppDbContext> dbOptions)
        {
            dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var dbContext = new AppDbContext(dbOptions);

            dbContext.Locations.Add(new Location
            {
                Id = 1,
                Name = "Main",
                CompanyId = 1,
                Address = "123 Test St",
                City = "Testville",
                State = "OH",
                ZipCode = "00000"
            });
            dbContext.Users.Add(new User { Id = 99, Name = "Maria", Email = "m@x.com", Role = "Employee", IsActive = true });
            dbContext.UserLocations.Add(new UserLocation { UserId = 99, LocationId = 1 });
            dbContext.Recipes.Add(new CulinaryCommandApp.Recipe.Entities.Recipe
            {
                RecipeId = 1, LocationId = 1, Title = "Eggs Benedict",
                Category = "Brunch", RecipeType = "Entree"
            });
            dbContext.SaveChanges();
            return dbContext;
        }

        private static AppDbContext BuildSeededDbContext()
            => BuildSeededDbContext(out _);
    }
}