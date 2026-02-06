using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using MediatR;

namespace Application.ShipmentServices.Queries
{
    public class GetShipmentEvents
    {
        public class Query : IRequest<Result<List<ShipmentEventDTO>>>
        {
            public Guid Id { get; set; }
        }
        
        public class GetShipmentEventsHandler : IRequestHandler<Query, Result<List<ShipmentEventDTO>>>
        {
            private readonly IUnitOfWork _unitOfWork;
            public GetShipmentEventsHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<ShipmentEventDTO>>> Handle(Query request, CancellationToken cancellationToken)
            {
                GetShipmentEventsValidator validator = new GetShipmentEventsValidator();
                var validationResult = validator.Validate(request);

                if (!validationResult.IsValid)
                    return Result<List<ShipmentEventDTO>>.Failure("Validation failed", validationResult.Errors.Select(e => e.ErrorMessage));

                var shipmentEvents = await _unitOfWork.ShipmentEvents.GetShipmentEventsByShipmentIdAsync(request.Id);

                if (shipmentEvents == null)
                    return Result<List<ShipmentEventDTO>>.Failure("Shipment events not found.");

                var shipmentEventDTOs = shipmentEvents.Select(e => new ShipmentEventDTO
                {
                    EventCode = e.EventCode,
                    EventTime = e.EventTime,
                    Payload = e.Payload,
                    CorrelationId = e.CorrelationId
                }).ToList();

                return Result<List<ShipmentEventDTO>>.Success(shipmentEventDTOs, "Shipment events retrieved successfully.");
            }
        }
    }
}