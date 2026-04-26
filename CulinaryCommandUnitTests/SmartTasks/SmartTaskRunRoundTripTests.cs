using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CulinaryCommand.Tests.SmartTask
{
    public class SmartTaskRunRoundTripTests
    {
        private static AppDbContext BuildInMemoryDbContext()
        {
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(dbOptions);
        }

        [Fact]
        public async Task PersistsAndReadsBackSmartTaskRun()
        {
            using var dbContext = BuildInMemoryDbContext();

            var location = new Location
            {
                Id = 1,
                Name = "Main",
                Address = "123 Test St",
                City = "Testville",
                State = "IA",
                ZipCode = "00000",
                CompanyId = 1
            };
            var triggeringUser = new User { Id = 9, Name = "Maria", Email = "m@x.com", Role = "Manager" };

            dbContext.Locations.Add(location);
            dbContext.Users.Add(triggeringUser);

            var smartTaskRun = new SmartTaskRun
            {
                LocationId = location.Id,
                TriggeredByUserId = triggeringUser.Id,
                ServiceDate = new DateOnly(2026, 5, 1),
                RecipeIdsJson = "[1,2,3]",
                CreatedTaskIdsJson = "[]",
                Status = nameof(SmartTaskRunStatus.Planned)
            };
            dbContext.SmartTaskRuns.Add(smartTaskRun);

            var ct = global::Xunit.TestContext.Current.CancellationToken;
            await dbContext.SaveChangesAsync(ct);

            var persistedRun = await dbContext.SmartTaskRuns.SingleAsync(ct);

            Assert.Equal(location.Id, persistedRun.LocationId);
            Assert.Equal("[1,2,3]", persistedRun.RecipeIdsJson);
            Assert.Equal(nameof(SmartTaskRunStatus.Planned), persistedRun.Status);
        }
    }
}