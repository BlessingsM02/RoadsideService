using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoadsideService.Models
{
    internal class RequestData
    {
        public string ServiceProviderId { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string ServiceProviderLatitude { get; set; }
        public string ServiceProviderLongitude { get; set; }
        public string DriverId { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }
}
