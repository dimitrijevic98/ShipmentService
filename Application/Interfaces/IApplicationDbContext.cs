using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces
{
    public interface IApplicationDbContext
    {
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentEvent> ShipmentEvents { get; set; }
        public DbSet<ShipmentDocument> ShipmentDocuments { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}