using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IShipmentEventRepository
    {
        Task<List<ShipmentEvent>> GetShipmentEventsByShipmentIdAsync(Guid shipmentId);
        void Add(ShipmentEvent shipmentEvent);
        void Remove(ShipmentEvent shipmentEvent);
        void Update(ShipmentEvent shipmentEvent);
    }
}