using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace Application.ShipmentServices.Commands
{
    public class UploadLabelValidator : AbstractValidator<UploadLabel>
    {
        public UploadLabelValidator()
        {
            RuleFor(x => x.ShipmentId).NotEmpty().WithMessage("ShipmentId is required.");
            RuleFor(x => x.FileName).NotEmpty().WithMessage("Label file is required.").Must(BeValidFileType)
                .WithMessage("Only PDF and JPG files are allowed."); ;
        }
 
        private bool BeValidFileType(string filename)
        {
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg" };
            var extension = Path.GetExtension(filename).ToLowerInvariant();
 
            return allowedExtensions.Contains(extension);
        }
    }
}