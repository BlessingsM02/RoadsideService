using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoadsideService.Models
{
    internal class Vehicle
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string VehicleDescription { get; set; }
        public string PlateNumber { get; set; }
    }
}
