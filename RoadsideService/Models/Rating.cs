using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoadsideService.Models
{
    internal class Rating
    {
        public string DriverId { get; set; }
        public string ServiceProviderId { get; set; }
        public int RatingValue { get; set; }
        public string RequestId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
