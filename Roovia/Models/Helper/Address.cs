using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Roovia.Models.Helper
{
    public class Address
    {
        [StringLength(200)]
        public string? Street { get; set; }

        [StringLength(20)]
        public string? UnitNumber { get; set; } // For apartment/unit number

        [StringLength(100)]
        public string? ComplexName { get; set; } // For complex/estate name

        [StringLength(100)]
        public string? BuildingName { get; set; } // For building name

        [StringLength(10)]
        public string? Floor { get; set; } // For building floor

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Suburb { get; set; } // Added suburb for South African addresses

        [StringLength(50)]
        public string? Province { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? GateCode { get; set; } // For secure complexes

        public bool IsResidential { get; set; } = true; // To distinguish between residential and business addresses
        public double? Latitude { get; set; } // For geo-coordinates
        public double? Longitude { get; set; } // For geo-coordinates

        [StringLength(500)]
        public string? DeliveryInstructions { get; set; } // Special instructions for delivery
    }

    // Keep the validator class - no changes needed
    public class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator()
        {
            RuleFor(address => address.Street)
                .NotEmpty().WithMessage("Street is required.")
                .MaximumLength(200).WithMessage("Street cannot exceed 200 characters.");

            RuleFor(address => address.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

            RuleFor(address => address.Province)
                .NotEmpty().WithMessage("Province is required.")
                .MaximumLength(50).WithMessage("Province cannot exceed 50 characters.");

            RuleFor(address => address.PostalCode)
                .NotEmpty().WithMessage("Postal Code is required.")
                .MaximumLength(10).WithMessage("Postal Code cannot exceed 10 characters.")
                .Matches(@"^\d{4}$").WithMessage("Postal Code must be in a valid format (4 digits)."); // Updated for South Africa format

            RuleFor(address => address.Country)
                .NotEmpty().WithMessage("Country is required.")
                .MaximumLength(50).WithMessage("Country cannot exceed 50 characters.");

            RuleFor(address => address.ComplexName)
                .MaximumLength(100).WithMessage("Complex name cannot exceed 100 characters.");

            RuleFor(address => address.UnitNumber)
                .MaximumLength(20).WithMessage("Unit number cannot exceed 20 characters.");

            RuleFor(address => address.BuildingName)
                .MaximumLength(100).WithMessage("Building name cannot exceed 100 characters.");

            RuleFor(address => address.Suburb)
                .MaximumLength(100).WithMessage("Suburb cannot exceed 100 characters.");

            RuleFor(address => address.DeliveryInstructions)
                .MaximumLength(500).WithMessage("Delivery instructions cannot exceed 500 characters.");
        }
    }
}