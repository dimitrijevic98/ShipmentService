using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class ShipmentEventRepository : IShipmentEventRepository
    {
        private readonly IApplicationDbContext _context;
        public ShipmentEventRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShipmentEvent>> GetShipmentEventsByShipmentIdAsync(Guid shipmentId)
        {
            return await _context.ShipmentEvents.Where(e => e.ShipmentId == shipmentId).OrderBy(e => e.EventTime).ToListAsync();
        }
        public void Add(ShipmentEvent shipmentEvent)
        {
            _context.ShipmentEvents.Add(shipmentEvent);
        }
        public void Remove(ShipmentEvent shipmentEvent)
        {
            _context.ShipmentEvents.Remove(shipmentEvent);
        }

        public void Update(ShipmentEvent shipmentEvent)
        {
            _context.ShipmentEvents.Update(shipmentEvent);
        }
    }
}