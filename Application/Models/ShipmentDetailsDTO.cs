using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.Models
{
    public class ShipmentDetailsDTO
    {
        public Guid Id { get; set; }
        public string ReferenceNumber { get; set; }
        public string SenderName { get; set; }
        public string RecipientName { get; set; }
        public ShipmentState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ShipmentEventDTO? LastStatus { get; set; }

        public List<ShipmentEventDTO> ShipmentEvents { get; set; } = new List<ShipmentEventDTO>();
    }
}