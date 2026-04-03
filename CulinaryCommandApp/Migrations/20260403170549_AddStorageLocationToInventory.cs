using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryCommand.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageLocationToInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Vendors",
                type: "bit(1)",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit(1)",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "bit(1)",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "TaskTemplates",
                type: "bit(1)",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "TaskLists",
                type: "bit(1)",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsSubRecipe",
                table: "Recipes",
                type: "bit(1)",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLocationLocked",
                table: "PurchaseOrders",
                type: "bit(1)",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(1)");

            migrationBuilder.AddColumn<int>(
                name: "StorageLocationId",
                table: "Ingredients",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InventoryItemDTO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SKU = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Unit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsLowStock = table.Column<bool>(type: "bit(1)", nullable: false),
                    OutOfStockDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastOrderDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    VendorName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VendorLogoUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StorageLocationId = table.Column<int>(type: "int", nullable: true),
                    StorageLocationName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemDTO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemDTO_StorageLocations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_StorageLocationId",
                table: "Ingredients",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemDTO_StorageLocationId",
                table: "InventoryItemDTO",
                column: "StorageLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_StorageLocations_StorageLocationId",
                table: "Ingredients",
                column: "StorageLocationId",
                principalTable: "StorageLocations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ingredients_StorageLocations_StorageLocationId",
                table: "Ingredients");

            migrationBuilder.DropTable(
                name: "InventoryItemDTO");

            migrationBuilder.DropTable(
                name: "StorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_StorageLocationId",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "StorageLocationId",
                table: "Ingredients");

            migrationBuilder.AlterColumn<sbyte>(
                name: "IsActive",
                table: "Vendors",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit(1)");

            migrationBuilder.AlterColumn<sbyte>(
                name: "IsActive",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit(1)");

            migrationBuilder.AlterColumn<sbyte>(
                name: "EmailConfirmed",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit(1)");

            migrationBuilder.AlterColumn<sbyte>(
                name: "IsActive",
                table: "TaskTemplates",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit(1)");

            migrationBuilder.AlterColumn<sbyte>(
                name: "IsActive",
                table: "TaskLists",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit(1)");

            migrationBuilder.AlterColumn<sbyte>(
                name: "IsSubRecipe",
                table: "Recipes",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit(1)");

            migrationBuilder.AlterColumn<sbyte>(
                name: "IsLocationLocked",
                table: "PurchaseOrders",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit(1)");
        }
    }
}
