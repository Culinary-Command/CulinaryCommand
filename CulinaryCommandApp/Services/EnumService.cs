using CulinaryCommandApp.Data.Enums;

namespace CulinaryCommand.Services
{
    public class EnumService
    {
        public List<string> GetCategories()
        {
            return new List<string>
            {
                Category.Produce,
                Category.DairyEggs,
                Category.Cheese,
                Category.MeatPoultry,
                Category.Seafood,
                Category.Bakery,
                Category.PastaGrains,
                Category.DryGoods,
                Category.Spices,
                Category.Condiments,
                Category.SyrupsMixes,
                Category.Desserts,
                Category.Beer,
                Category.Wine,
                Category.Spirits,
                Category.Beverages,
                Category.Prepared
            };
        }

        public List<string> GetRecipeTypes()
        {
            return new List<string>
            {
                RecipeType.Appetizer,
                RecipeType.Entree,
                RecipeType.Side,
                RecipeType.Dessert,
                RecipeType.Sauce,
                RecipeType.PrepItem
            };
        }

        public List<string> GetUnits() => new()
        {
            Units.Percent,
            Units.Each,
            Units.Grams,
            Units.Kilograms,
            Units.Ounces,
            Units.Pounds,
            Units.Milliliters,
            Units.Liters,
            Units.Teaspoon,
            Units.Tablespoon,
            Units.Cup,
            Units.Quart,
            Units.Gallon,
            Units.Serving
        };
    }
}