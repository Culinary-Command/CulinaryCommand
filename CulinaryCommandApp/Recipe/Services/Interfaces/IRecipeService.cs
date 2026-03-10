using Rec = CulinaryCommandApp.Recipe.Entities;

namespace CulinaryCommandApp.Recipe.Services.Interfaces
{
    public interface IRecipeService
    {
        Task<List<Rec.Recipe>> GetAllAsync();
        Task<List<Rec.Recipe>> GetAllByLocationIdAsync(int locationId);
        Task<Rec.Recipe?> GetByIdAsync(int id);

        /// <summary>
        /// Returns the recipe with the given <paramref name="id"/> only when its
        /// <c>LocationId</c> is contained in <paramref name="allowedLocationIds"/>.
        /// Returns <c>null</c> when the recipe does not exist or the caller is not
        /// permitted to access its location.
        /// </summary>
        Task<Rec.Recipe?> GetByIdAsync(int id, IEnumerable<int> allowedLocationIds);

        Task CreateAsync(Rec.Recipe recipe);
        Task UpdateAsync(Rec.Recipe recipe);
        Task DeleteAsync(int id);

        /// <summary>
        /// Returns only the current <c>RowVersion</c> token for the recipe with the
        /// given <paramref name="id"/>, or <c>null</c> when the recipe does not exist.
        /// Use this to refresh the concurrency token without overwriting the caller's
        /// in-flight edits.
        /// </summary>
        Task<DateTime?> GetRowVersionAsync(int id);

        /// <summary>
        /// Recursively flattens all raw ingredient lines of a recipe (including sub-recipes)
        /// into a single list with quantities scaled by <paramref name="multiplier"/>.
        /// Throws <see cref="InvalidOperationException"/> if a circular reference is detected.
        /// </summary>
        Task<List<FlatIngredientLine>> FlattenIngredientsAsync(int recipeId, decimal multiplier = 1m, CancellationToken ct = default);
    }

    /// <summary>
    /// A fully-resolved ingredient line with no further nesting — used for cost rollup
    /// and inventory deduction when a recipe is produced.
    /// </summary>
    public sealed class FlatIngredientLine
    {
        public int IngredientId { get; init; }
        public string IngredientName { get; init; } = string.Empty;
        public decimal Quantity { get; init; }
        public int UnitId { get; init; }
        public string UnitName { get; init; } = string.Empty;
    }
}
