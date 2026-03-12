using Microsoft.EntityFrameworkCore;
using CulinaryCommand.Data.Entities;
using CulinaryCommandApp.Inventory.Entities;
using CulinaryCommandApp.Recipe.Entities;
using PO = CulinaryCommand.PurchaseOrder.Entities;
using V = CulinaryCommand.Vendor.Entities;


namespace CulinaryCommand.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Location> Locations => Set<Location>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Tasks> Tasks => Set<Tasks>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CulinaryCommandApp.Inventory.Entities.Ingredient> Ingredients => Set<CulinaryCommandApp.Inventory.Entities.Ingredient>();
        public DbSet<CulinaryCommandApp.Recipe.Entities.Recipe> Recipes => Set<CulinaryCommandApp.Recipe.Entities.Recipe>();
        public DbSet<CulinaryCommandApp.Recipe.Entities.RecipeIngredient> RecipeIngredients => Set<CulinaryCommandApp.Recipe.Entities.RecipeIngredient>();
        public DbSet<CulinaryCommandApp.Recipe.Entities.RecipeStep> RecipeSteps => Set<CulinaryCommandApp.Recipe.Entities.RecipeStep>();
        public DbSet<CulinaryCommandApp.Recipe.Entities.RecipeSubRecipe> RecipeSubRecipes => Set<CulinaryCommandApp.Recipe.Entities.RecipeSubRecipe>();
        public DbSet<UserLocation> UserLocations => Set<UserLocation>();
        public DbSet<ManagerLocation> ManagerLocations => Set<ManagerLocation>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<CulinaryCommandApp.Inventory.Entities.Unit> Units => Set<CulinaryCommandApp.Inventory.Entities.Unit>();
        public DbSet<PO.PurchaseOrder> PurchaseOrders => Set<PO.PurchaseOrder>();
        public DbSet<PO.PurchaseOrderLine> PurchaseOrderLines => Set<PO.PurchaseOrderLine>();
        public DbSet<V.Vendor> Vendors => Set<V.Vendor>();
        public DbSet<V.LocationVendor> LocationVendors => Set<V.LocationVendor>();
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<LocationUnit> LocationUnits => Set<LocationUnit>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Employees)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Location>()
                .HasOne(l => l.Company)
                .WithMany(c => c.Locations)
                .HasForeignKey(l => l.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Explicit join: Employees
            modelBuilder.Entity<UserLocation>()
                .HasKey(ul => new { ul.UserId, ul.LocationId });

            modelBuilder.Entity<UserLocation>()
                .HasOne(ul => ul.User)
                .WithMany(u => u.UserLocations)
                .HasForeignKey(ul => ul.UserId);

            modelBuilder.Entity<UserLocation>()
                .HasOne(ul => ul.Location)
                .WithMany(l => l.UserLocations)
                .HasForeignKey(ul => ul.LocationId);


            // Explicit join: Managers 
            modelBuilder.Entity<ManagerLocation>()
                .HasKey(ml => new { ml.UserId, ml.LocationId });

            modelBuilder.Entity<ManagerLocation>()
                .HasOne(ml => ml.User)
                .WithMany(u => u.ManagerLocations)
                .HasForeignKey(ml => ml.UserId);

            modelBuilder.Entity<ManagerLocation>()
                .HasOne(ml => ml.Location)
                .WithMany(l => l.ManagerLocations)
                .HasForeignKey(ml => ml.LocationId);

            // Ingredient belongs to a Location
            modelBuilder.Entity<CulinaryCommandApp.Inventory.Entities.Ingredient>()
                .HasOne(i => i.Location)
                .WithMany()
                .HasForeignKey(i => i.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ingredient optionally belongs to a Vendor
            modelBuilder.Entity<CulinaryCommandApp.Inventory.Entities.Ingredient>()
                .HasOne(i => i.Vendor)
                .WithMany()
                .HasForeignKey(i => i.VendorId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Vendor belongs to a Company
            modelBuilder.Entity<V.Vendor>()
                .HasOne(v => v.Company)
                .WithMany(c => c.Vendors)
                .HasForeignKey(v => v.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // LocationVendor: composite PK
            modelBuilder.Entity<V.LocationVendor>()
                .HasKey(lv => new { lv.LocationId, lv.VendorId });

            modelBuilder.Entity<V.LocationVendor>()
                .HasOne(lv => lv.Location)
                .WithMany(l => l.LocationVendors)
                .HasForeignKey(lv => lv.LocationId);

            modelBuilder.Entity<V.LocationVendor>()
                .HasOne(lv => lv.Vendor)
                .WithMany(v => v.LocationVendors)
                .HasForeignKey(lv => lv.VendorId);

            // Recipe belongs to a Location
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.Recipe>()
                .HasOne(r => r.Location)
                .WithMany(l => l.Recipes)
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // RowVersion is a MySQL timestamp(6) with DEFAULT/ON UPDATE CURRENT_TIMESTAMP(6).
            // Mark it as database-generated so EF never sends DateTime.MinValue on INSERT/UPDATE.
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.Recipe>()
                .Property(r => r.RowVersion)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // Tasks optionally references a Recipe (prep task)
            modelBuilder.Entity<Tasks>()
                .HasOne(t => t.Recipe)
                .WithMany()
                .HasForeignKey(t => t.RecipeId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // RecipeIngredient to parent Recipe
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.RecipeIngredient>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // RecipeIngredient - optional Ingredient (raw ingredient line)
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.RecipeIngredient>()
                .HasOne(ri => ri.Ingredient)
                .WithMany()
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // RecipeIngredient - optional sub-Recipe
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.RecipeIngredient>()
                .HasOne(ri => ri.SubRecipe)
                .WithMany()
                .HasForeignKey(ri => ri.SubRecipeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // RecipeIngredient - Unit
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.RecipeIngredient>()
                .HasOne(ri => ri.Unit)
                .WithMany()
                .HasForeignKey(ri => ri.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // RecipeStep - Recipe
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.RecipeStep>()
                .HasOne(rs => rs.Recipe)
                .WithMany(r => r.Steps)
                .HasForeignKey(rs => rs.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // RecipeSubRecipe: composite PK
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.RecipeSubRecipe>()
                .HasKey(rs => new { rs.ParentRecipeId, rs.ChildRecipeId });

            // RecipeSubRecipe - parent Recipe (cascade: deleting a parent removes its sub-recipe links)
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.RecipeSubRecipe>()
                .HasOne(rs => rs.ParentRecipe)
                .WithMany(r => r.SubRecipeUsages)
                .HasForeignKey(rs => rs.ParentRecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // RecipeSubRecipe - child Recipe (restrict: cannot delete a sub-recipe still in use)
            modelBuilder.Entity<CulinaryCommandApp.Recipe.Entities.RecipeSubRecipe>()
                .HasOne(rs => rs.ChildRecipe)
                .WithMany(r => r.UsedInRecipes)
                .HasForeignKey(rs => rs.ChildRecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureLocationUnit(modelBuilder);    
        }

        private void ConfigureLocationUnit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocationUnit>()
                .HasKey(lu => new { lu.LocationId, lu.UnitId });
            
            modelBuilder.Entity<LocationUnit>()
                .HasOne(lu => lu.Location)
                .WithMany(l => l.LocationUnits)
                .HasForeignKey(lu => lu.LocationId);

            modelBuilder.Entity<LocationUnit>()
                .HasOne(lu => lu.Unit)
                .WithMany(u => u.LocationUnits)
                .HasForeignKey(lu => lu.UnitId);
        }
    }
}