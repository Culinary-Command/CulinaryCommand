using CulinaryCommand.Data;
using Rec = CulinaryCommandApp.Recipe.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryCommandApp.Recipe.Services
{
    public class RecipeService
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
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.RecipeId == id);
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
    }
}