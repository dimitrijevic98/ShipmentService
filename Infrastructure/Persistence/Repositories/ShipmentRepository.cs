using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class ShipmentRepository : IShipmentRepository
    {
        private readonly IApplicationDbContext _context;
        public ShipmentRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ShipmentRefNumExists(string referenceNumber)
        {
           return await _context.Shipments.AnyAsync(s => s.ReferenceNumber == referenceNumber);
        }

        public async Task<List<Shipment>> GetAllShipmentsAsync(int page, int pageSize, ShipmentState? state)
        {
            var query = _context.Shipments.AsNoTracking();

            if (state.HasValue)
                query = query.Where(s => s.State == state.Value);
            
            var shipments = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return shipments;
        }

        public async Task<Shipment> GetShipmentByIdAsync(Guid id)
        {
            return await _context.Shipments.Include(s => s.ShipmentEvents).FirstOrDefaultAsync(s => s.Id == id);
        }

        public void Add(Shipment shipment)
        {
            _context.Shipments.Add(shipment);
        }

        public void Remove(Shipment shipment)
        {
            _context.Shipments.Remove(shipment);
        }

        public void Update(Shipment shipment)
        {
            _context.Shipments.Update(shipment);
        }
    }
}