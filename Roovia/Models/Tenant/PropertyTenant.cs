using FluentValidation;
using Roovia.Models.Helper;

namespace Roovia.Models.Tenant
{
    public class PropertyTenant
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public int PropertyId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IdNumber { get; set; }
        public string EmailAddress { get; set; }
        public string IsEmailNotificationsEnabled { get; set; }
        public string MobileNumber { get; set; }
        public string IsSmsNotificationsEnabled { get; set; }
        public BankAccount BankAccount { get; set; } = new BankAccount();
        public Address Address { get; set; } = new Address();
        public int DebitDayOfMonth { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }

        public DateTime UpdatedDate { get; set; }

        public Guid UpdatedBy { get; set; }

        public bool? isRemoved { get; set; } = false;

        public DateTime? RemovedDate { get; set; }

        public Guid? RemovedBy { get; set; }
        public PropertyTenant()
        {
            BankAccount = new();
            Address = new();
        }
    }

    public class PropertyTenantValidator : AbstractValidator<PropertyTenant>
    {
        public PropertyTenantValidator()
        {
            RuleFor(tenant => tenant.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");
            RuleFor(tenant => tenant.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");
            RuleFor(tenant => tenant.IdNumber)
                .NotEmpty().WithMessage("ID number is required.")
                .Matches(@"^\d{13}$").WithMessage("ID number must be a valid 13-digit number.");
            RuleFor(tenant => tenant.EmailAddress)
                .EmailAddress().WithMessage("Email address must be a valid email address.")
                .When(tenant => !string.IsNullOrEmpty(tenant.EmailAddress));
            RuleFor(tenant => tenant.MobileNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Mobile number must be a valid phone number.")
                .When(tenant => !string.IsNullOrEmpty(tenant.MobileNumber));
            RuleFor(tenant => tenant.DebitDayOfMonth)
                .InclusiveBetween(1, 31).WithMessage("Debit day of the month must be between 1 and 31.");
        }
    }
}
