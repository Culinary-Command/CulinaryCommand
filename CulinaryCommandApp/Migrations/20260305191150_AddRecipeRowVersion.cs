using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryCommand.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL timestamp range starts at 1970-01-01; the EF-generated default of
            // 0001-01-01 is rejected. Use raw SQL with CURRENT_TIMESTAMP(6) so that
            // both new and existing rows receive a valid value, and ON UPDATE keeps
            // the column in sync for optimistic concurrency.
            migrationBuilder.Sql(
                "ALTER TABLE `Recipes` ADD `RowVersion` timestamp(6) NOT NULL " +
                "DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Recipes");
        }
    }
}
