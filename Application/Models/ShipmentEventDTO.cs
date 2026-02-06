using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Models
{
    public class ShipmentEventDTO
    {
        public string EventCode { get; set; }
        public DateTime EventTime { get; set; }
        public string? Payload { get; set; }
        public string CorrelationId { get; set; }
    }
}