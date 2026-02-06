using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Azure
{
    public class LabelUploadedMessage
    {
         public Guid ShipmentId { get; set; }
        public string BlobName { get; set; }
        public string CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}