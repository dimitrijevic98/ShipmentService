using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ShipmentEvent
    {
        public Guid Id { get; set; }
        public Guid ShipmentId { get; set; }
        public Shipment Shipment { get; set; }
        public string EventCode { get; set; }
        public DateTime EventTime { get; set; }
        public string? Payload { get; set; }
        public string CorrelationId { get; set; }
    }
}