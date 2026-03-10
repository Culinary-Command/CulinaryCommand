using CulinaryCommand.Data;
using CulinaryCommandApp.Recipe.Services.Interfaces;
using Rec = CulinaryCommandApp.Recipe.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryCommandApp.Recipe.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly AppDbContext _db;

        public RecipeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Rec.Recipe>> GetAllAsync()
            => await _db.Recipes
                .Include(r => r.RecipeIngredients)
                .Include(r => r.Steps)
                .ToListAsync();

        public async Task<List<Rec.Recipe>> GetAllByLocationIdAsync(int locationId)
        {
            return await _db.Recipes
                .Where(r => r.LocationId == locationId)
                .Include(r => r.RecipeIngredients)
                .Include(r => r.Steps)
                .ToListAsync();
        }

        public Task<Rec.Recipe?> GetByIdAsync(int id)
        {
            return _db.Recipes
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Unit)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.SubRecipe)
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.RecipeId == id);
        }

        public Task<Rec.Recipe?> GetByIdAsync(int id, IEnumerable<int> allowedLocationIds)
        {
            return _db.Recipes
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Unit)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.SubRecipe)
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.RecipeId == id && allowedLocationIds.Contains(r.LocationId));
        }

        public async Task CreateAsync(Rec.Recipe recipe)
        {
            if (string.IsNullOrWhiteSpace(recipe.Category))
                throw new Exception("Category is required.");

            _db.Recipes.Add(recipe);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Rec.Recipe recipe)
        {
            _db.Recipes.Update(recipe);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var recipe = await _db.Recipes.FindAsync(id);
            if (recipe != null)
            {
                _db.Recipes.Remove(recipe);
                await _db.SaveChangesAsync();
            }
        }

        // ── Ingredient Flattening ────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<DateTime?> GetRowVersionAsync(int id)
        {
            return await _db.Recipes
                .AsNoTracking()
                .Where(r => r.RecipeId == id)
                .Select(r => r.RowVersion)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task<List<FlatIngredientLine>> FlattenIngredientsAsync(
            int recipeId, decimal multiplier = 1m, CancellationToken ct = default)
        {
            return await FlattenCoreAsync(recipeId, multiplier, new HashSet<int>(), ct);
        }

        private async Task<List<FlatIngredientLine>> FlattenCoreAsync(
            int recipeId, decimal multiplier, HashSet<int> visited, CancellationToken ct)
        {
            if (!visited.Add(recipeId))
                throw new InvalidOperationException(
                    $"Circular sub-recipe reference detected at recipe ID {recipeId}.");

            var recipe = await _db.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Unit)
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct);

            if (recipe is null)
                throw new InvalidOperationException($"Recipe ID {recipeId} not found.");

            var result = new List<FlatIngredientLine>();

            foreach (var line in recipe.RecipeIngredients)
            {
                var scaledQty = line.Quantity * multiplier;

                if (line.SubRecipeId.HasValue)
                {
                    // Recurse into sub-recipe — scale by the quantity used on this line
                    var subLines = await FlattenCoreAsync(line.SubRecipeId.Value, scaledQty, visited, ct);
                    result.AddRange(subLines);
                }
                else if (line.IngredientId.HasValue && line.Ingredient is not null)
                {
                    result.Add(new FlatIngredientLine
                    {
                        IngredientId   = line.IngredientId.Value,
                        IngredientName = line.Ingredient.Name,
                        Quantity       = scaledQty,
                        UnitId         = line.UnitId,
                        UnitName       = line.Unit?.Name ?? string.Empty,
                    });
                }
            }

            // Allow the same sub-recipe to appear in multiple branches (diamond dep)
            visited.Remove(recipeId);

            return result;
        }
    }
}
