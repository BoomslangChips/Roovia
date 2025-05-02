using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roovia.Models.Helper
{
    public class ContactNumber
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string? Number { get; set; }

        public ContactNumberType Type { get; set; } = ContactNumberType.Mobile;

        [StringLength(50)]
        public string? Description { get; set; } // e.g., "Work", "Home", etc.

        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;

        // Entity relationship fields
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; } // "User", "Company", "Branch"

        public int? RelatedEntityId { get; set; } // For Company and Branch entities

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; } // For User entities

        // Navigation properties to support relationships (optional)
        public string? ApplicationUserId { get; set; }
        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Helper method to set related entity regardless of ID type
        public void SetRelatedEntity(string type, object id)
        {
            RelatedEntityType = type;

            if (id is int intId)
            {
                RelatedEntityId = intId;
                RelatedEntityStringId = null;

                if (type == "Company")
                    CompanyId = intId;
                else if (type == "Branch")
                    BranchId = intId;
            }
            else
            {
                RelatedEntityStringId = id.ToString();
                RelatedEntityId = null;

                if (type == "User")
                    ApplicationUserId = id.ToString();
            }
        }
    }

    public enum ContactNumberType
    {
        Mobile,
        Landline,
        Fax,
        WhatsApp,
        Other
    }

    // Validator class with necessary updates
    public class ContactNumberValidator : AbstractValidator<ContactNumber>
    {
        public ContactNumberValidator()
        {
            RuleFor(contact => contact.Number)
                .NotEmpty().WithMessage("Contact number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Contact number must be a valid phone number.");

            RuleFor(contact => contact.Description)
                .MaximumLength(50).WithMessage("Description cannot exceed 50 characters.");

            RuleFor(contact => contact.RelatedEntityType)
                .NotEmpty().WithMessage("Related entity type is required.")
                .Must(type => type == "User" || type == "Company" || type == "Branch")
                .WithMessage("Related entity type must be User, Company, or Branch.");

            // Updated validator to check either RelatedEntityId or RelatedEntityStringId is set
            RuleFor(contact => contact)
                .Must(c => c.RelatedEntityId.HasValue || !string.IsNullOrEmpty(c.RelatedEntityStringId))
                .WithMessage("Related entity ID is required.");
        }
    }
}