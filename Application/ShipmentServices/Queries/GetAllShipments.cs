using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.ShipmentServices.Queries
{
    public class GetAllShipments
    {
        public class Query : IRequest<Result<PagedResult<ShipmentDTO>>>
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public ShipmentState? State { get; set; }
        }

        public class GetAllShipmentsHandler : IRequestHandler<Query, Result<PagedResult<ShipmentDTO>>>
        {
            private readonly IUnitOfWork _unitOfWork;
            public GetAllShipmentsHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<PagedResult<ShipmentDTO>>> Handle(Query request, CancellationToken cancellationToken)
            {
                GetAllShipmentsValidator validator = new GetAllShipmentsValidator();
                var validationResult = validator.Validate(request);

                if (!validationResult.IsValid)
                    return Result<PagedResult<ShipmentDTO>>.Failure("Validation failed", validationResult.Errors.Select(e => e.ErrorMessage));

                var shipments = await _unitOfWork.Shipments.GetAllShipmentsAsync(request.Page, request.PageSize, request.State);

                if(shipments == null) 
                    return Result<PagedResult<ShipmentDTO>>.Failure("Shipment list is null.");
                
                var totalCount = shipments.Count;

                var shipmentDTOs = shipments.Select(s => new ShipmentDTO
                {
                    ReferenceNumber = s.ReferenceNumber,
                    SenderName = s.SenderName,
                    RecipientName = s.RecipientName,
                    State = s.State,
                    CreatedAt = s.CreatedAt
                });

                var pagedResult = new PagedResult<ShipmentDTO>(shipmentDTOs, totalCount, request.Page, request.PageSize);

                return Result<PagedResult<ShipmentDTO>>.Success(pagedResult, "Shipments retrieved successfully.");
            }
        }
        
    }
}