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
            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Name", "Abbreviation", "ConversionFactor" },
                values: new object[,]
                {
                    { "Percent",     "%",    1m        },
                    { "Each",        "ea",   1m        },
                    { "Grams",       "g",    1m        },
                    { "Kilograms",   "kg",   1000m     },
                    { "Ounces",      "oz",   28.3495m  },
                    { "Pounds",      "lb",   453.592m  },
                    { "Milliliters", "mL",   1m        },
                    { "Liters",      "L",    1000m     },
                    { "Teaspoon",    "tsp",  4.92892m  },
                    { "Tablespoon",  "tbsp", 14.7868m  },
                    { "Cup",         "cup",  236.588m  },
                    { "Quart",       "qt",   946.353m  },
                    { "Gallon",      "gal",  3785.41m  },
                    { "Serving",     "srv",  1m        },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "Name",
                keyValues: new object[]
                {
                    "Percent", "Each", "Grams", "Kilograms", "Ounces", "Pounds",
                    "Milliliters", "Liters", "Teaspoon", "Tablespoon", "Cup",
                    "Quart", "Gallon", "Serving"
                });
        }
    }
}
