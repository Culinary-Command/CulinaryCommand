using System.ComponentModel.DataAnnotations;

namespace CulinaryCommand.Inventory.DTOs
{
    public class CreateIngredientDTO
    {
        [Required]
        public string Name { get; set; } = default!;
        public string? SKU { get; set; }
        public decimal CurrentQuantity { get; set; } = 0;
        public decimal Price { get; set; } = 0m;
        public string? Category { get; set; }
        public decimal ReorderLevel { get; set; } = 0m;
        [Range(1, int.MaxValue, ErrorMessage = "UnitId must be a positive integer.")]
        public int UnitId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "LocationId must be a positive integer.")]
        public int LocationId { get; set; }
        public int? VendorId { get; set; }
    }
}