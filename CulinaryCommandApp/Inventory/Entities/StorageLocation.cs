using System.Collections.Generic;
using CulinaryCommandApp.Inventory.DTOs;

namespace CulinaryCommandApp.Inventory.Entities
{
    public class StorageLocation
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<InventoryItemDTO> InventoryItems { get; set; } = new();
    }
}
