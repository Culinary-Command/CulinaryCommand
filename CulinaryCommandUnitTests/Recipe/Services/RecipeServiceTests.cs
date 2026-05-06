using System;
using System.Linq;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommandApp.Inventory.Entities;
using CulinaryCommandApp.Recipe.Services;
using Microsoft.EntityFrameworkCore;
using Rec = CulinaryCommandApp.Recipe.Entities;

namespace CulinaryCommand.Tests.Recipe;

public class RecipeServiceTests
{
    [Fact]
    public async Task FlattenIngredientsAsync_FlattensNestedSubRecipesAndScalesQuantities()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();

        SeedLocation(dbContext);
        dbContext.Units.AddRange(
            new Unit { Id = 1, Name = "Gram", Abbreviation = "g", ConversionFactor = 1m },
            new Unit { Id = 2, Name = "Each", Abbreviation = "ea", ConversionFactor = 1m });
        dbContext.Ingredients.AddRange(
            new Ingredient { Id = 11, Name = "Flour", LocationId = 1, UnitId = 1 },
            new Ingredient { Id = 12, Name = "Eggs", LocationId = 1, UnitId = 2 });
        dbContext.Recipes.AddRange(
            new Rec.Recipe { RecipeId = 100, LocationId = 1, Title = "Dough", Category = "Prep" },
            new Rec.Recipe { RecipeId = 101, LocationId = 1, Title = "Pizza", Category = "Entree" });
        dbContext.RecipeIngredients.AddRange(
            new Rec.RecipeIngredient { RecipeId = 100, IngredientId = 11, UnitId = 1, Quantity = 3m, SortOrder = 1 },
            new Rec.RecipeIngredient { RecipeId = 101, IngredientId = 12, UnitId = 2, Quantity = 2m, SortOrder = 1 },
            new Rec.RecipeIngredient { RecipeId = 101, SubRecipeId = 100, UnitId = 1, Quantity = 4m, SortOrder = 2 });
        await dbContext.SaveChangesAsync(ct);

        var service = new RecipeService(dbContext);

        var lines = await service.FlattenIngredientsAsync(101, multiplier: 2m, ct: ct);
        var linesByIngredient = lines.ToDictionary(line => line.IngredientName);

        Assert.Equal(2, lines.Count);
        Assert.Equal(24m, linesByIngredient["Flour"].Quantity);
        Assert.Equal(1, linesByIngredient["Flour"].UnitId);
        Assert.Equal("Gram", linesByIngredient["Flour"].UnitName);
        Assert.Equal(4m, linesByIngredient["Eggs"].Quantity);
        Assert.Equal(2, linesByIngredient["Eggs"].UnitId);
        Assert.Equal("Each", linesByIngredient["Eggs"].UnitName);
    }

    [Fact]
    public async Task FlattenIngredientsAsync_ThrowsWhenRecipeDoesNotExist()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();
        var service = new RecipeService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.FlattenIngredientsAsync(999, ct: ct));

        Assert.Contains("999", exception.Message);
    }

    [Fact]
    public async Task FlattenIngredientsAsync_ThrowsWhenSubRecipesAreCircular()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();

        SeedLocation(dbContext);
        dbContext.Units.Add(new Unit { Id = 1, Name = "Each", Abbreviation = "ea", ConversionFactor = 1m });
        dbContext.Recipes.AddRange(
            new Rec.Recipe { RecipeId = 1, LocationId = 1, Title = "Parent", Category = "Prep" },
            new Rec.Recipe { RecipeId = 2, LocationId = 1, Title = "Child", Category = "Prep" });
        dbContext.RecipeIngredients.AddRange(
            new Rec.RecipeIngredient { RecipeId = 1, SubRecipeId = 2, UnitId = 1, Quantity = 1m, SortOrder = 1 },
            new Rec.RecipeIngredient { RecipeId = 2, SubRecipeId = 1, UnitId = 1, Quantity = 1m, SortOrder = 1 });
        await dbContext.SaveChangesAsync(ct);

        var service = new RecipeService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.FlattenIngredientsAsync(1, ct: ct));

        Assert.Contains("Circular", exception.Message);
    }

    [Fact]
    public async Task FlattenIngredientsAsync_AllowsSharedSubRecipeInSeparateBranches()
    {
        var ct = global::Xunit.TestContext.Current.CancellationToken;
        using var dbContext = BuildDbContext();

        SeedLocation(dbContext);
        dbContext.Units.Add(new Unit { Id = 1, Name = "Gram", Abbreviation = "g", ConversionFactor = 1m });
        dbContext.Ingredients.Add(new Ingredient { Id = 21, Name = "Salt", LocationId = 1, UnitId = 1 });
        dbContext.Recipes.AddRange(
            new Rec.Recipe { RecipeId = 200, LocationId = 1, Title = "Shared Base", Category = "Prep" },
            new Rec.Recipe { RecipeId = 201, LocationId = 1, Title = "Branch A", Category = "Prep" },
            new Rec.Recipe { RecipeId = 202, LocationId = 1, Title = "Branch B", Category = "Prep" },
            new Rec.Recipe { RecipeId = 203, LocationId = 1, Title = "Main", Category = "Entree" });
        dbContext.RecipeIngredients.AddRange(
            new Rec.RecipeIngredient { RecipeId = 200, IngredientId = 21, UnitId = 1, Quantity = 5m, SortOrder = 1 },
            new Rec.RecipeIngredient { RecipeId = 201, SubRecipeId = 200, UnitId = 1, Quantity = 2m, SortOrder = 1 },
            new Rec.RecipeIngredient { RecipeId = 202, SubRecipeId = 200, UnitId = 1, Quantity = 3m, SortOrder = 1 },
            new Rec.RecipeIngredient { RecipeId = 203, SubRecipeId = 201, UnitId = 1, Quantity = 1m, SortOrder = 1 },
            new Rec.RecipeIngredient { RecipeId = 203, SubRecipeId = 202, UnitId = 1, Quantity = 1m, SortOrder = 2 });
        await dbContext.SaveChangesAsync(ct);

        var service = new RecipeService(dbContext);

        var lines = await service.FlattenIngredientsAsync(203, ct: ct);

        Assert.Equal(2, lines.Count);
        Assert.All(lines, line => Assert.Equal("Salt", line.IngredientName));
        Assert.Equal(25m, lines.Sum(line => line.Quantity));
    }

    private static AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedLocation(AppDbContext dbContext)
    {
        dbContext.Locations.Add(new Location
        {
            Id = 1,
            Name = "Main Kitchen",
            CompanyId = 1,
            Address = "123 Test St",
            City = "Testville",
            State = "IA",
            ZipCode = "50000"
        });
    }
}
