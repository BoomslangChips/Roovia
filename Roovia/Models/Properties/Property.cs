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
        public DateTime? LeaseOriginalStartDate { get; set; } = DateTime.Now;
        public DateTime? CurrentLeaseStartDate { get; set; } = DateTime.Now;
        public DateTime? LeaseEndDate { get; set; } = DateTime.Now;

        public void EnsureValidDates()
        {
            if (LeaseOriginalStartDate == DateTime.MinValue)
            {
                LeaseOriginalStartDate = DateTime.Now;
            }

            if (CurrentLeaseStartDate == DateTime.MinValue)
            {
                CurrentLeaseStartDate = DateTime.Now;
            }

            if (LeaseEndDate == DateTime.MinValue)
            {
                LeaseEndDate = DateTime.Now;
            }
        }
        public int? CurrentTenantId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public Guid? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public Guid? UpdatedBy { get; set; }
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
