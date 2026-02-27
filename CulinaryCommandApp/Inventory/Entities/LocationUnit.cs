using System.Text.Json.Serialization;
using CulinaryCommand.Data.Entities;
using CulinaryCommandApp.Inventory.Entities;

namespace CulinaryCommandApp.Inventory.Entities
{
    public class LocationUnit
    {
        [JsonIgnore]
        public Location Location { get; set; } = default!;
        public int LocationId { get; set; }

        [JsonIgnore]
        public Unit Unit { get; set; } = default!;
        public int UnitId { get; set; }
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}