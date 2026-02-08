using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.ShipmentServices.Commands
{
    public class CreateShipment : IRequest<Result<Guid>>
    {
        public string ReferenceNumber { get; set; }
        public string SenderName { get; set; }
        public string RecipientName { get; set; }
        
    }
    public class CreateShipmentHandler : IRequestHandler<CreateShipment, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public CreateShipmentHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<Guid>> Handle(CreateShipment request, CancellationToken cancellationToken)
        {
            CreateShipmentValidator validator = new CreateShipmentValidator();
            var validationResult = validator.Validate(request);
            
            if (!validationResult.IsValid)
                return Result<Guid>.Failure("Validation failed", validationResult.Errors.Select(e => e.ErrorMessage));

            var refNumExists = await _unitOfWork.Shipments.ShipmentRefNumExists(request.ReferenceNumber);

            if(refNumExists) 
                return Result<Guid>.Failure($"Shipment with reference number {request.ReferenceNumber} already exists.");

            var shipment = new Shipment
            {
                ReferenceNumber = request.ReferenceNumber,
                SenderName = request.SenderName,
                RecipientName = request.RecipientName,
                State = ShipmentState.Created,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var shipmentEvent = new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                EventCode = "CREATED",
                EventTime = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };

            shipment.ShipmentEvents.Add(shipmentEvent);

            _unitOfWork.Shipments.Add(shipment);
            
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
            
            if(!result) throw new Exception("Failed to create shipment.");

            return Result<Guid>.Success(shipment.Id, "Shipment created successfully.");
        }
    }
}