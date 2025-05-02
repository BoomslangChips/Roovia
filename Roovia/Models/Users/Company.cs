using FluentValidation;
using Roovia.Models.Helper;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roovia.Models.Users
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? VatNumber { get; set; }

        public Address Address { get; set; } = new Address();
        public DateTime? CreatedOn { get; set; }
        public bool IsActive { get; set; } = true;

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(450)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public List<Branch>? Branches { get; set; } = new List<Branch>();
        public List<ApplicationUser>? Users { get; set; } = new List<ApplicationUser>();
        public List<Email> EmailAddresses { get; set; } = new List<Email>();
        public List<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();

        // Helper methods to get primary contact info
        public string? GetPrimaryEmail() => EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
        public string? GetPrimaryContactNumber() => ContactNumbers?.FirstOrDefault(c => c.IsPrimary)?.Number;
    }

    // Keep the validator class - no changes needed
    public class CompanyValidator : AbstractValidator<Company>
    {
        private readonly IEmailValidator _emailValidator;
        private readonly IContactNumberValidator _contactNumberValidator;

        public CompanyValidator(IEmailValidator emailValidator, IContactNumberValidator contactNumberValidator)
        {
            _emailValidator = emailValidator;
            _contactNumberValidator = contactNumberValidator;

            RuleFor(company => company.Name)
                .NotEmpty().WithMessage("Company name is required.")
                .MaximumLength(100).WithMessage("Company name must not exceed 100 characters.");

            RuleFor(company => company.RegistrationNumber)
                .NotEmpty().WithMessage("Registration number is required.")
                .MaximumLength(50).WithMessage("Registration number must not exceed 50 characters.");

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

            // Check that at least one email address exists and is marked as primary
            RuleFor(company => company.EmailAddresses)
                .Must(emails => emails != null && emails.Any(e => e.IsPrimary))
                .WithMessage("At least one primary email address is required.");

            // Check that at least one contact number exists and is marked as primary
            RuleFor(company => company.ContactNumbers)
                .Must(contacts => contacts != null && contacts.Any(c => c.IsPrimary))
                .WithMessage("At least one primary contact number is required.");
        }
    }

    // Interfaces for DI
    public interface IEmailValidator
    {
        bool Validate(Email email);
    }

    public interface IContactNumberValidator
    {
        bool Validate(ContactNumber contactNumber);
    }
}