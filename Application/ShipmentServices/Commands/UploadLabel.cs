using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.ShipmentServices.Commands
{
    public class UploadLabel : IRequest<Result<Guid>>
    {
        public Guid ShipmentId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public Stream FileStream { get; set; }
        public long FileSize { get; set; }
    }

    public class UploadLabelHandler : IRequestHandler<UploadLabel, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBlobService _blobService;
        private readonly IServiceBus _serviceBus;
        private readonly ILogger<UploadLabelHandler> _logger;

        public UploadLabelHandler(IUnitOfWork unitOfWork,IBlobService blobService, IServiceBus serviceBus, ILogger<UploadLabelHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _blobService = blobService;
            _serviceBus = serviceBus;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(UploadLabel request, CancellationToken cancellationToken)
        {
            UploadLabelValidator validator = new UploadLabelValidator();
            var validationResult = validator.Validate(request);
 
            if (!validationResult.IsValid)
                return Result<Guid>.Failure("Invalid command parameters", validationResult.Errors.Select(e => e.ErrorMessage).ToList());

            var shipment = await _unitOfWork.Shipments.GetShipmentByIdAsync(request.ShipmentId);

            if (shipment == null)
                return Result<Guid>.Failure("Shipment not found.");

            if (shipment.State != ShipmentState.Created)
                return Result<Guid>.Failure("Label can be uploaded only for Created shipments");

            var correlationId = shipment.ShipmentEvents.FirstOrDefault(e => e.EventCode == "CREATED").CorrelationId ?? Guid.NewGuid().ToString();

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var blobName = $"{shipment.Id}/{Guid.NewGuid()}_{request.FileName}";

            try
            {
                await _blobService.UploadAsync(blobName, request.FileStream, request.ContentType);

                var document = new ShipmentDocument
                {
                    ShipmentId = shipment.Id,
                    BlobName = blobName,
                    ContentType = request.ContentType,
                    SizeBytes = request.FileSize,
                    UploadedAt = DateTime.UtcNow
                };

                shipment.Document = document;

                var shipmentEvent = new ShipmentEvent
                {
                    ShipmentId = shipment.Id,
                    EventCode = "LABEL_UPLOADED",
                    EventTime = DateTime.UtcNow,
                    Payload = $"Label {blobName} uploaded.",
                    CorrelationId = correlationId
                };

                shipment.ShipmentEvents.Add(shipmentEvent);

                shipment.State = ShipmentState.LabelUploaded;
                shipment.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
 
                await _serviceBus.PublishAsync(shipment.Id, blobName, correlationId);

                await transaction.CommitAsync(cancellationToken);

                return Result<Guid>.Success(document.Id, "Label uploaded successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                await _blobService.DeleteIfExistsAsync(blobName);

                _logger.LogInformation("Failed to upload label for ShipmentId={ShipmentId}, CorrelationId={CorrelationId}", 
                    shipment.Id, correlationId);
                
                throw;
            }

        }
    }

}