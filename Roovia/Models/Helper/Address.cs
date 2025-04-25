using FluentValidation;

namespace Roovia.Models.Helper
{
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

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
                .MaximumLength(4).WithMessage("Postal Code cannot exceed 4 characters.")
                .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Postal Code must be in a valid format.");

            RuleFor(address => address.Country)
                .NotEmpty().WithMessage("Country is required.")
                .MaximumLength(50).WithMessage("Country cannot exceed 50 characters.");
        }
    }
}
