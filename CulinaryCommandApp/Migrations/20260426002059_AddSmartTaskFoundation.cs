using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryCommand.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartTaskFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneratedBy",
                table: "Tasks",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "SmartTaskRunId",
                table: "Tasks",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "PrepLeadTimeMinutes",
                table: "Recipes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ServiceTimeOverride",
                table: "Recipes",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceWindow",
                table: "Recipes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SmartTaskRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TriggeredByUserId = table.Column<int>(type: "int", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecipeIdsJson = table.Column<string>(type: "TEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedTaskIdsJson = table.Column<string>(type: "TEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LambdaRequestId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartTaskRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartTaskRuns_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmartTaskRuns_Users_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_SmartTaskRunId",
                table: "Tasks",
                column: "SmartTaskRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartTaskRuns_LocationId",
                table: "SmartTaskRuns",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartTaskRuns_TriggeredByUserId",
                table: "SmartTaskRuns",
                column: "TriggeredByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmartTaskRuns");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_SmartTaskRunId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "GeneratedBy",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SmartTaskRunId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PrepLeadTimeMinutes",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ServiceTimeOverride",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ServiceWindow",
                table: "Recipes");
        }
    }
}
