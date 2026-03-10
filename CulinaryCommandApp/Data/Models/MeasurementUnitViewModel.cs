using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CulinaryCommand.Models
{
    public class MeasurementUnitViewModel
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = "";
        public string Abbreviation { get; set; } = "";
    }

}