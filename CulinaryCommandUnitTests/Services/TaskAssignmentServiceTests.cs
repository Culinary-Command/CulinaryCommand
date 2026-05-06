using System;
using System.Linq;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommand.Data.Enums;
using CulinaryCommand.Services;
using CulinaryCommandApp.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = CulinaryCommand.Data.Enums.TaskStatus;
using Rec = CulinaryCommandApp.Recipe.Entities;

namespace CulinaryCommand.Tests.Services;

public class TaskAssignmentServiceTests
{
    [Fact]
    public async Task GetByLocationAsync_ReturnsTasksForLocationOrderedByDueDateDescending()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedLocationGraph(dbContext);

        var recipe = BuildRecipe(100, locationId: 1);
        dbContext.Recipes.Add(recipe);
        dbContext.RecipeSteps.Add(new Rec.RecipeStep
        {
            StepId = 1000,
            RecipeId = recipe.RecipeId,
            StepNumber = 1,
            Instructions = "Mix ingredients"
        });
        dbContext.RecipeIngredients.Add(new Rec.RecipeIngredient
        {
            RecipeIngredientId = 1001,
            RecipeId = recipe.RecipeId,
            IngredientId = 10,
            UnitId = 1,
            Quantity = 2m,
            SortOrder = 1
        });

        dbContext.Tasks.AddRange(
            BuildTask(1, 1, 5, dueDate: new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc), recipeId: 100, ingredientId: 10),
            BuildTask(2, 1, 5, dueDate: new DateTime(2026, 5, 4, 8, 0, 0, DateTimeKind.Utc), recipeId: 100, ingredientId: 10),
            BuildTask(3, 2, 5, dueDate: new DateTime(2026, 5, 5, 8, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync(ct);

        var service = new TaskAssignmentService(dbContext);

        var tasks = await service.GetByLocationAsync(1);

        Assert.Equal(new[] { 2, 1 }, tasks.Select(task => task.Id).ToArray());
        Assert.All(tasks, task => Assert.Equal(1, task.LocationId));
        var latestTask = tasks[0];
        Assert.NotNull(latestTask.User);
        Assert.NotNull(latestTask.Recipe);
        Assert.Single(latestTask.Recipe!.Steps);
        Assert.Single(latestTask.Recipe.RecipeIngredients);
        var ingredientLine = latestTask.Recipe.RecipeIngredients.Single();
        Assert.NotNull(ingredientLine.Ingredient);
        Assert.NotNull(ingredientLine.Unit);
        Assert.NotNull(latestTask.Ingredient);
    }

    [Fact]
    public async Task CreateAsync_SetsTimestampsAndPersistsTask()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedLocationGraph(dbContext);
        await dbContext.SaveChangesAsync(ct);

        var service = new TaskAssignmentService(dbContext);
        var before = DateTime.UtcNow;

        var created = await service.CreateAsync(new Tasks
        {
            Name = "Prep soup",
            Station = "Prep",
            Status = DomainTaskStatus.Pending,
            Assigner = "Chef",
            Date = new DateTime(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 5, 2, 15, 0, 0, DateTimeKind.Utc),
            LocationId = 1,
            UserId = 5,
            Kind = WorkTaskKind.Generic
        });
        var after = DateTime.UtcNow;

        Assert.True(created.CreatedAt >= before && created.CreatedAt <= after);
        Assert.True(created.UpdatedAt >= before && created.UpdatedAt <= after);
        Assert.True((created.UpdatedAt - created.CreatedAt).Duration() < TimeSpan.FromSeconds(1));
        Assert.Equal(1, await dbContext.Tasks.CountAsync(ct));
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatusAndTimestampForExistingTask()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedLocationGraph(dbContext);

        var existingTask = BuildTask(1, 1, 5, dueDate: new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc));
        existingTask.UpdatedAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        dbContext.Tasks.Add(existingTask);
        await dbContext.SaveChangesAsync(ct);

        var service = new TaskAssignmentService(dbContext);

        await service.UpdateStatusAsync(1, DomainTaskStatus.Completed);

        var stored = await dbContext.Tasks.SingleAsync(ct);
        Assert.Equal(DomainTaskStatus.Completed, stored.Status);
        Assert.True(stored.UpdatedAt > new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task BumpDueDateAsync_AddsRequestedDays()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedLocationGraph(dbContext);

        var dueDate = new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc);
        dbContext.Tasks.Add(BuildTask(1, 1, 5, dueDate));
        await dbContext.SaveChangesAsync(ct);

        var service = new TaskAssignmentService(dbContext);

        await service.BumpDueDateAsync(1, 3);

        var stored = await dbContext.Tasks.SingleAsync(ct);
        Assert.Equal(dueDate.AddDays(3), stored.DueDate);
    }

    [Fact]
    public async Task GetForUserAsync_AppliesOptionalLocationFilterAndOrdersAscending()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedLocationGraph(dbContext);

        dbContext.Tasks.AddRange(
            BuildTask(1, 1, 5, dueDate: new DateTime(2026, 5, 4, 8, 0, 0, DateTimeKind.Utc)),
            BuildTask(2, 1, 5, dueDate: new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc)),
            BuildTask(3, 2, 5, dueDate: new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc)),
            BuildTask(4, 1, 6, dueDate: new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync(ct);

        var service = new TaskAssignmentService(dbContext);

        var tasks = await service.GetForUserAsync(5, locationId: 1);

        Assert.Equal(new[] { 2, 1 }, tasks.Select(task => task.Id).ToArray());
        Assert.All(tasks, task =>
        {
            Assert.Equal(5, task.UserId);
            Assert.Equal(1, task.LocationId);
        });
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingTaskAndIgnoresMissingId()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        SeedLocationGraph(dbContext);

        dbContext.Tasks.Add(BuildTask(1, 1, 5, new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync(ct);

        var service = new TaskAssignmentService(dbContext);

        await service.DeleteAsync(999);
        await service.DeleteAsync(1);

        Assert.Empty(await dbContext.Tasks.ToListAsync(ct));
    }

    private static AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedLocationGraph(AppDbContext dbContext)
    {
        dbContext.Locations.AddRange(
            new Location
            {
                Id = 1,
                Name = "Main Kitchen",
                CompanyId = 1,
                Address = "123 Test St",
                City = "Testville",
                State = "IA",
                ZipCode = "50000"
            },
            new Location
            {
                Id = 2,
                Name = "Second Kitchen",
                CompanyId = 1,
                Address = "456 Test St",
                City = "Testville",
                State = "IA",
                ZipCode = "50001"
            });
        dbContext.Users.AddRange(
            new User { Id = 5, Name = "Jordan", Email = "jordan@example.com", Role = "Employee", IsActive = true },
            new User { Id = 6, Name = "Avery", Email = "avery@example.com", Role = "Employee", IsActive = true });
        dbContext.Units.Add(new Unit { Id = 1, Name = "Each", Abbreviation = "ea", ConversionFactor = 1m });
        dbContext.Ingredients.Add(new Ingredient
        {
            Id = 10,
            Name = "Eggs",
            LocationId = 1,
            UnitId = 1,
            Category = "Dairy"
        });
    }

    private static Rec.Recipe BuildRecipe(int recipeId, int locationId) =>
        new()
        {
            RecipeId = recipeId,
            LocationId = locationId,
            Title = "Omelette",
            Category = "Breakfast",
            RecipeType = "Entree"
        };

    private static Tasks BuildTask(int id, int locationId, int userId, DateTime dueDate, int? recipeId = null, int? ingredientId = null) =>
        new()
        {
            Id = id,
            Name = $"Task {id}",
            Station = "Prep",
            Status = DomainTaskStatus.Pending,
            Assigner = "Chef",
            Date = new DateTime(2026, 5, 2, 7, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 5, 2, 7, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 5, 2, 7, 0, 0, DateTimeKind.Utc),
            DueDate = dueDate,
            LocationId = locationId,
            UserId = userId,
            Kind = recipeId.HasValue ? WorkTaskKind.PrepFromRecipe : WorkTaskKind.Generic,
            RecipeId = recipeId,
            IngredientId = ingredientId
        };
}
