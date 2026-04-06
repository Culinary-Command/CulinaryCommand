using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryCommand.Migrations
{
    /// <inheritdoc />
    public partial class FixIngredientUnitRelation : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
