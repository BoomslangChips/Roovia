using FluentValidation;
using Roovia.Models.Helper;
using System.Collections.Generic;

namespace Roovia.Models.Users
{
    public class Company
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }

        public string? RegistrationNumber { get; set; }

        public string? ContactNumber { get; set; }

        public string? Email { get; set; }

        public Address Address { get; set; } = new Address();

        public string? Website { get; set; }

        public string? VatNumber { get; set; }

        public DateTime? CreatedOn { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public List<Branch>? Branches { get; set; } = new List<Branch>();
        public List<ApplicationUser>? Users { get; set; } = new List<ApplicationUser>();

    }

    public class CompanyValidator : AbstractValidator<Company>
    {
        public CompanyValidator()
        {
            RuleFor(company => company.Name)
                .NotEmpty().WithMessage("Company name is required.")
                .MaximumLength(100).WithMessage("Company name must not exceed 100 characters.");

            RuleFor(company => company.RegistrationNumber)
                .NotEmpty().WithMessage("Registration number is required.")
                .MaximumLength(50).WithMessage("Registration number must not exceed 50 characters.");

            RuleFor(company => company.ContactNumber)
                .NotEmpty().WithMessage("Contact number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Contact number must be a valid phone number.");

            RuleFor(company => company.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(company => company.Website)
          .MaximumLength(200).WithMessage("Website URL must not exceed 200 characters.")
          .Matches(@"^(https?:\/\/)?([\w\-]+\.)+[\w\-]+(\/[\w\-]*)*$").WithMessage("Website must be a valid URL.")
          .When(company => !string.IsNullOrEmpty(company.Website));

            RuleFor(company => company.VatNumber)
                .MaximumLength(50).WithMessage("VAT number must not exceed 50 characters.")
                .When(company => !string.IsNullOrEmpty(company.VatNumber));

            RuleFor(company => company.CreatedOn)
                .NotEmpty().WithMessage("CreatedOn date is required.")
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("CreatedOn date cannot be in the future.");

            RuleFor(company => company.CreatedBy)
                .NotEmpty().WithMessage("CreatedBy is required.");


        }
    }
}
