namespace CulinaryCommandApp.Recipe.Entities
{
    public class RecipeSubRecipe
    {
        // composite PK configured via Fluent API in AppDbContext
        public int ParentRecipeId { get; set; }
        public Recipe? ParentRecipe { get; set; }

        public int ChildRecipeId { get; set; }
        public Recipe? ChildRecipe { get; set; }
    }
}