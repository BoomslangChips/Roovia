using FluentValidation;
using Roovia.Models.Helper;
using Roovia.Models.Properties;

namespace Roovia.Models.PropertyOwner
{
    public class PropertyOwner
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string IdNumber { get; set; }

        public string VatNumber { get; set; }

        public string EmailAddress { get; set; }

        public string IsEmailNotificationsEnabled { get; set; }

        public string MobileNumber { get; set; }

        public string IsSmsNotificationsEnabled { get; set; }

        public BankAccount BankAccount { get; set; } = new BankAccount();

        public Address Address { get; set; } = new Address();

        public DateTime CreatedOn { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime UpdatedDate { get; set; }

        public Guid UpdatedBy { get; set; }

        public List<Property> Properties { get; set; } = new List<Property>();

    }

    public class BeneficiaryValidator : AbstractValidator<PropertyOwner>
    {
        public BeneficiaryValidator()
        {
            RuleFor(beneficiary => beneficiary.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");
            RuleFor(beneficiary => beneficiary.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");
            RuleFor(beneficiary => beneficiary.IdNumber)
                .NotEmpty().WithMessage("ID number is required.")
                .Matches(@"^\d{13}$").WithMessage("ID number must be a valid 13-digit number.");
            RuleFor(beneficiary => beneficiary.VatNumber)
                .MaximumLength(50).WithMessage("VAT number must not exceed 50 characters.")
                .When(beneficiary => !string.IsNullOrEmpty(beneficiary.VatNumber));
            RuleFor(beneficiary => beneficiary.EmailAddress)
                .EmailAddress().WithMessage("Email address must be a valid email address.")
                .When(beneficiary => !string.IsNullOrEmpty(beneficiary.EmailAddress));
            RuleFor(beneficiary => beneficiary.MobileNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Mobile number must be a valid phone number.")
                .When(beneficiary => !string.IsNullOrEmpty(beneficiary.MobileNumber));
        }
    }
}
