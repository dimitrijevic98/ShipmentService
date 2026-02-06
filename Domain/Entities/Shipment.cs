using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class Shipment
    {
        public Guid Id { get; set; }
        public string ReferenceNumber { get; set; }
        public string SenderName { get; set; }
        public string RecipientName { get; set; }
        public ShipmentState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<ShipmentEvent> ShipmentEvents { get; set; } = new List<ShipmentEvent>();
        public ShipmentDocument? Document { get; set; }
    }
}