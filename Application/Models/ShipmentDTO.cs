using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.Models
{
    public class ShipmentDTO
    {
        public Guid Id { get; set; }
        public string ReferenceNumber { get; set; }
        public string SenderName { get; set; }
        public string RecipientName { get; set; }
        public ShipmentState State { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}