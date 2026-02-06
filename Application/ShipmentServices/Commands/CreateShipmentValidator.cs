using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace Application.ShipmentServices.Commands
{
    public class CreateShipmentValidator : AbstractValidator<CreateShipment>
    {
        public CreateShipmentValidator()
        {
            RuleFor(x => x.ReferenceNumber)
                .NotEmpty().WithMessage("Reference number is required.");

            RuleFor(x => x.SenderName)
                .NotEmpty().WithMessage("Sender name is required.");

            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("Recipient name is required.");
        }
        
    }
}