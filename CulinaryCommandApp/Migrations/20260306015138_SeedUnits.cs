using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryCommand.Migrations
{
    /// <inheritdoc />
    public partial class SeedUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Negative IDs are reserved for seeded/system units so they never
            // collide with user-created units (which use the positive identity sequence).
            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Name", "Abbreviation", "ConversionFactor" },
                values: new object[,]
                {
                    { -1,  "Percent",     "%",    1m        },
                    { -2,  "Each",        "ea",   1m        },
                    { -3,  "Grams",       "g",    1m        },
                    { -4,  "Kilograms",   "kg",   1000m     },
                    { -5,  "Ounces",      "oz",   28.3495m  },
                    { -6,  "Pounds",      "lb",   453.592m  },
                    { -7,  "Milliliters", "mL",   1m        },
                    { -8,  "Liters",      "L",    1000m     },
                    { -9,  "Teaspoon",    "tsp",  4.92892m  },
                    { -10, "Tablespoon",  "tbsp", 14.7868m  },
                    { -11, "Cup",         "cup",  236.588m  },
                    { -12, "Quart",       "qt",   946.353m  },
                    { -13, "Gallon",      "gal",  3785.41m  },
                    { -14, "Serving",     "srv",  1m        },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete by PK (Id) so we only remove the exact seeded rows and
            // never accidentally touch user-created units that share a name.
            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "Id",
                keyValues: new object[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12, -13, -14 });
        }
    }
}
