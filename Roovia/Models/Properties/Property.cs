using FluentValidation;
using Roovia.Models.Helper;

namespace Roovia.Models.Properties
{
    public class Property
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public Address Address { get; set; } = new Address();

        public decimal RentalAmount { get; set; }
        public bool HasTenant { get; set; }
        public DateTime LeaseOriginalStartDate { get; set; }
        public DateTime CurrentLeaseStartDate { get; set; }
        public DateTime LeaseEndDate { get; set; }
        public Guid CurrentTenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }

        public DateTime UpdatedDate { get; set; }

        public DateTime UpdatedBy { get; set; }
    }

    public class PropertyValidator : AbstractValidator<Property>
    {
        public PropertyValidator()
        {
            RuleFor(property => property.Address)
                .NotNull().WithMessage("Address is required.")
                .SetValidator(new AddressValidator());
            RuleFor(property => property.HasTenant)
                .NotNull().WithMessage("Has Tenant is required.");
            RuleFor(property => property.LeaseOriginalStartDate)
                .NotEmpty().WithMessage("Lease original start date is required.");
            RuleFor(property => property.CurrentLeaseStartDate)
                .NotEmpty().WithMessage("Current lease start date is required.");
            RuleFor(property => property.LeaseEndDate)
                .NotEmpty().WithMessage("Lease end date is required.");
        }
    }
}
