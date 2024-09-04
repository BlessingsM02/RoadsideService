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
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double ServiceProviderLatitude { get; set; }
        public double ServiceProviderLongitude { get; set; }
        public double Amount { get; set; }
        public string DriverId { get; set; }
        public string Status { get; set; }
        public string RatingId { get; set; }
        public DateTime Date { get; set; }
    }
}
