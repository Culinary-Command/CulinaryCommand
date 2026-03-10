using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CulinaryCommand.Data;
using CulinaryCommand.Inventory.Entities;
using CulinaryCommand.Inventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CulinaryCommand.Inventory.Services
{
    public class IngredientService : IIngredientService
    {
        private readonly AppDbContext _db;

        public IngredientService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Ingredients
                .AsNoTracking()
                .Select(ingredient => ingredient.Category)
                .Distinct()
                .OrderBy(category => category)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Ingredient>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            return await _db.Ingredients
                .AsNoTracking()
                .Include(ingredient => ingredient.Unit)
                .Where(ingredient => ingredient.Category == category)
                .OrderBy(ingredient => ingredient.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _db.Ingredients
                    .AsNoTracking()
                    .Include(ingredient => ingredient.Unit)
                    .OrderBy(i => i.Name)
                    .ToListAsync(cancellationToken);

        public async Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            await _db.Ingredients
                    .AsNoTracking()
                    .Include(ingredient => ingredient.Unit)
                    .FirstOrDefaultAsync(ingredient => ingredient.Id == id, cancellationToken);

        public async Task<Ingredient> CreateAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
        {
            await _db.Ingredients.AddAsync(ingredient, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return ingredient;
        }

        public async Task UpdateAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
        {
            _db.Ingredients.Update(ingredient);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Ingredients.FirstOrDefaultAsync(ingredient => ingredient.Id == id, cancellationToken);
            if (entity != null)
            {
                _db.Ingredients.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}