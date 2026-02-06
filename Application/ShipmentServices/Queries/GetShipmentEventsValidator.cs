using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace Application.ShipmentServices.Queries
{
    public class GetShipmentEventsValidator : AbstractValidator<GetShipmentEvents.Query>
    {
        public GetShipmentEventsValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Shipment ID is required.");
        }
    }
}