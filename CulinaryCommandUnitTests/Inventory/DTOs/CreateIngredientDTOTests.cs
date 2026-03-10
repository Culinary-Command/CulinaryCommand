using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CulinaryCommand.Components.Pages.Recipes;
using CulinaryCommand.Inventory.DTOs;
using Xunit;


namespace CulinaryCommandUnitTests.Inventory.DTOs
{
    public class CreateIngredientDTOTests
    {
        /* Helper function that runs DataAnnotations validation on a DTO and returns any validation fails*/
        private static IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void defaults_are_correct()
        {
            var DTO = new CreateIngredientDTO();

            Assert.Null(DTO.Name);
            Assert.Null(DTO.SKU);
            Assert.Null(DTO.Category);
            Assert.Equal(0m, DTO.CurrentQuantity);
            Assert.Equal(0m, DTO.Price);
            Assert.Equal(0m, DTO.ReorderLevel);
            Assert.Equal(0, DTO.UnitId);
        }

        [Fact]
        public void valid_dto_passes_validation()
        {
            var DTO = new CreateIngredientDTO
            {
                Name = "Flour",
                UnitId = 1,
                LocationId = 1,
                CurrentQuantity = 10m,
                Price = 2.99m,
                Category = "Baking",
                ReorderLevel = 1m
            };

            var results = Validate(DTO);
            Assert.Empty(results);
        }

        [Fact]
        public void missing_name_fails_validation()
        {
            var DTO = new CreateIngredientDTO
            {
                Name = string.Empty,
                UnitId = 1
            };

            var results = Validate(DTO);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains("Name"));
        }

        [Fact]
        public void unitid_must_be_positive()
        {
            var DTO = new CreateIngredientDTO
            {
                Name = "Flour",
                UnitId = 0
            };

            var results = Validate(DTO);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains("UnitId"));
            Assert.Contains(results, r => r.ErrorMessage?.Contains("UnitId must be a positive integer") ?? false);
        }
    }
}