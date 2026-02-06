using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace Application.ShipmentServices.Queries
{
    public class GetShipmentDetailsValidator : AbstractValidator<GetShipmentDetails.Query>
    {
        public GetShipmentDetailsValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Shipment Id must not be empty.");
        }
    }
}