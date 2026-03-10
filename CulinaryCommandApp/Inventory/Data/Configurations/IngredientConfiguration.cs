using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CulinaryCommand.Inventory.Entities;

namespace CulinaryCommand.Inventory.Data.Configurations
{
    public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
    {
        public void Configure(EntityTypeBuilder<Ingredient> builder)
        {
            // map to existing table
            builder.ToTable("Ingredients");

            // model builder explicitly state Id is the primary key
            builder.HasKey(ingredient => ingredient.Id);

            // reference IngredientId
            builder
                .Property(ingredient => ingredient.Id)
                .HasColumnName("IngredientId");

            // require name and restrict to 200 characters
            builder
                .Property(ingredient => ingredient.Name)
                .HasMaxLength(200).IsRequired();

            // configure how StockQuantity is stored
            builder
                .Property(ingredient => ingredient.StockQuantity)
                .HasColumnType("decimal(18, 4)");

            // sets up a one-to-many relationship between ingredient and unit
            builder
                .HasOne(ingredient => ingredient.Unit)
                .WithMany(unit => unit.Ingredients)
                .HasForeignKey(ingredient => ingredient.UnitId);

            // requires category to be max 100 characters
            builder
                .Property(ingredient => ingredient.Category)
                .HasMaxLength(100);
        }
    }
}
