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
    public class GetShipmentDetails
    {
        public class Query : IRequest<Result<ShipmentDetailsDTO>>
        {
            public Guid Id { get; set; }
        }

        public class GetShipmentDetailsHandler : IRequestHandler<Query, Result<ShipmentDetailsDTO>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetShipmentDetailsHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<ShipmentDetailsDTO>> Handle(Query request, CancellationToken cancellationToken)
            {
                GetShipmentDetailsValidator validator = new GetShipmentDetailsValidator();
                var validationResult = validator.Validate(request);

                if(!validationResult.IsValid)
                    return Result<ShipmentDetailsDTO>.Failure("Validation failed", validationResult.Errors.Select(e => e.ErrorMessage));
                

                var shipment = await _unitOfWork.Shipments.GetShipmentByIdAsync(request.Id);

                if(shipment == null) 
                    return Result<ShipmentDetailsDTO>.Failure($"Shipment with ID {request.Id} not found.");
                    // throw new NotFoundException($"Shipment with ID {request.Id} not found.");

                var lastEvent = shipment.ShipmentEvents
                    .OrderByDescending(e => e.EventTime)
                    .FirstOrDefault();

                var shipmentDto = new ShipmentDetailsDTO
                {
                    Id = shipment.Id,
                    ReferenceNumber = shipment.ReferenceNumber,
                    SenderName = shipment.SenderName,
                    RecipientName = shipment.RecipientName,
                    State = shipment.State,
                    CreatedAt = shipment.CreatedAt,
                    UpdatedAt = shipment.UpdatedAt,

                    LastStatus = lastEvent == null ? null : new ShipmentEventDTO
                    {
                        EventCode = lastEvent.EventCode,
                        EventTime = lastEvent.EventTime,
                        Payload = lastEvent.Payload,
                        CorrelationId = lastEvent.CorrelationId
                    },

                    ShipmentEvents = shipment.ShipmentEvents
                        .OrderByDescending(e => e.EventTime)
                        .Select(e => new ShipmentEventDTO
                        {
                            EventCode = e.EventCode,
                            EventTime = e.EventTime,
                            Payload = e.Payload,
                            CorrelationId = e.CorrelationId
                        })
                        .ToList()
                };
                
                return Result<ShipmentDetailsDTO>.Success(shipmentDto, "Shipment details retrieved successfully.");
            }
        }



    }
}