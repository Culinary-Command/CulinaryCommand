using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CulinaryCommand.Data.Entities
{
        public class Location
        {
                [Key]
                public int Id { get; set; }

                [Required, MaxLength(256)]
                public string Name { get; set; }

                [Required, MaxLength(256)]
                public string Address { get; set; }

                [Required, MaxLength(256)]
                public string City { get; set; }

                [Required, MaxLength(256)]
                public string State { get; set; }

                [Required, MaxLength(256)]
                public string ZipCode { get; set; }

                // marginEdgeKey should be nullable, not every location will have one
                public string? MarginEdgeKey { get; set; }

                // keep company nullable until we add a way to create companies
                public int CompanyId { get; set; }
                public Company Company { get; set; }

                public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

                // join table combining employees and locations
                [JsonIgnore]
                public ICollection<UserLocation> UserLocations { get; set; } = new List<UserLocation>();

                // join table combining managers and locations
                [JsonIgnore]
                public ICollection<ManagerLocation> ManagerLocations { get; set; } = new List<ManagerLocation>();

                [NotMapped]
                [JsonIgnore]
                // so you can still call Location.Managers and get the list
                public IEnumerable<User> Managers => ManagerLocations.Select(ml => ml.User);

                [NotMapped]
                [JsonIgnore]
                public IEnumerable<User> Employees => UserLocations.Select(ul => ul.User);

                [JsonIgnore]
                public ICollection<CulinaryCommand.Vendor.Entities.LocationVendor> LocationVendors { get; set; } = new List<CulinaryCommand.Vendor.Entities.LocationVendor>();

        }

}
