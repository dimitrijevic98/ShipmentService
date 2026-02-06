using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces
{
    public interface IShipmentRepository
    {
        Task<bool> ShipmentRefNumExists(string referenceNumber);
        Task<List<Shipment>> GetAllShipmentsAsync(int page, int pageSize, ShipmentState? state);
        Task<Shipment> GetShipmentByIdAsync(Guid id);
        void Add(Shipment shipment);
        void Remove(Shipment shipment);
        void Update(Shipment shipment);
    }
}