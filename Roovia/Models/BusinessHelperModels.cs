using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using Roovia.Models.UserCompanyModels;
using Roovia.Models.BusinessMappingModels;
using Microsoft.AspNetCore.Mvc.Formatters;
using Roovia.Models.UserCompanyMappingModels;

namespace Roovia.Models.BusinessHelperModels
{
    #region Address

    public class Address
    {
        [StringLength(200)]
        public string? Street { get; set; }

        [StringLength(20)]
        public string? UnitNumber { get; set; }

        [StringLength(100)]
        public string? ComplexName { get; set; }

        [StringLength(100)]
        public string? BuildingName { get; set; }

        [StringLength(10)]
        public string? Floor { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Suburb { get; set; }

        [StringLength(50)]
        public string? Province { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? Country { get; set; } = "South Africa";

        [StringLength(20)]
        public string? GateCode { get; set; }

        public bool IsResidential { get; set; } = true;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [StringLength(500)]
        public string? DeliveryInstructions { get; set; }
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
                .MaximumLength(10).WithMessage("Postal Code cannot exceed 10 characters.")
                .Matches(@"^\d{4}$").WithMessage("Postal Code must be in a valid format (4 digits).");

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

    #endregion

    #region BankAccount

    public class BankAccount
    {
        [StringLength(100)]
        public string? AccountType { get; set; }

        [StringLength(20)]
        public string? AccountNumber { get; set; }

        public int? BankNameId { get; set; } // FK to BankNameType

        [StringLength(10)]
        public string? BranchCode { get; set; }

        [ForeignKey("BankNameId")]
        public virtual BankNameType? BankName { get; set; }
    }

    public class BankAccountValidator : AbstractValidator<BankAccount>
    {
        public BankAccountValidator()
        {
            RuleFor(account => account.AccountType)
                .NotEmpty().WithMessage("Account type is required.")
                .MaximumLength(100).WithMessage("Account name must not exceed 100 characters.");

            RuleFor(account => account.AccountNumber)
                .NotEmpty().WithMessage("Account number is required.")
                .Matches(@"^\d{1,20}$").WithMessage("Account number must be a valid numeric value.");

            RuleFor(account => account.BranchCode)
                .NotEmpty().WithMessage("Branch code is required.")
                .Matches(@"^\d{1,10}$").WithMessage("Branch code must be a valid numeric value.");

            RuleFor(account => account.BankNameId)
                .NotEmpty().WithMessage("Bank name is required.");
        }
    }

    #endregion

    #region ContactNumber

    public class ContactNumber
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string? Number { get; set; }

        public int ContactNumberTypeId { get; set; } // FK to ContactNumberType

        [StringLength(50)]
        public string? Description { get; set; }

        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;

        // Entity relationship fields
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; }

        // Navigation properties to support relationships
        public string? ApplicationUserId { get; set; }
        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }
        public int? PropertyOwnerId { get; set; }
        public int? PropertyTenantId { get; set; }
        public int? PropertyBeneficiaryId { get; set; }
        public int? VendorId { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation property
        [ForeignKey("ContactNumberTypeId")]
        public virtual ContactNumberType? ContactNumberType { get; set; }

        // Helper method to set related entity regardless of ID type
        public void SetRelatedEntity(string type, object id)
        {
            RelatedEntityType = type;

            if (id is int intId)
            {
                RelatedEntityId = intId;
                RelatedEntityStringId = null;

                switch (type)
                {
                    case "Company":
                        CompanyId = intId;
                        break;
                    case "Branch":
                        BranchId = intId;
                        break;
                    case "PropertyOwner":
                        PropertyOwnerId = intId;
                        break;
                    case "PropertyTenant":
                        PropertyTenantId = intId;
                        break;
                    case "PropertyBeneficiary":
                        PropertyBeneficiaryId = intId;
                        break;
                    case "Vendor":
                        VendorId = intId;
                        break;
                }
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
                .Must(type => new[] { "User", "Company", "Branch", "PropertyOwner", "PropertyTenant", "PropertyBeneficiary", "Vendor" }.Contains(type))
                .WithMessage("Related entity type must be valid.");

            RuleFor(contact => contact)
                .Must(c => c.RelatedEntityId.HasValue || !string.IsNullOrEmpty(c.RelatedEntityStringId))
                .WithMessage("Related entity ID is required.");

            RuleFor(contact => contact.ContactNumberTypeId)
                .NotEmpty().WithMessage("Contact number type is required.");
        }
    }

    #endregion

    #region Email

    public class Email
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string? EmailAddress { get; set; }

        [StringLength(50)]
        public string? Description { get; set; }

        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;

        // Entity relationship fields
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; }

        // Navigation properties to support relationships
        public string? ApplicationUserId { get; set; }
        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }
        public int? PropertyOwnerId { get; set; }
        public int? PropertyTenantId { get; set; }
        public int? PropertyBeneficiaryId { get; set; }
        public int? VendorId { get; set; }

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

                switch (type)
                {
                    case "Company":
                        CompanyId = intId;
                        break;
                    case "Branch":
                        BranchId = intId;
                        break;
                    case "PropertyOwner":
                        PropertyOwnerId = intId;
                        break;
                    case "PropertyTenant":
                        PropertyTenantId = intId;
                        break;
                    case "PropertyBeneficiary":
                        PropertyBeneficiaryId = intId;
                        break;
                    case "Vendor":
                        VendorId = intId;
                        break;
                }
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

    public class EmailValidator : AbstractValidator<Email>
    {
        public EmailValidator()
        {
            RuleFor(email => email.EmailAddress)
                .NotEmpty().WithMessage("Email address is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

            RuleFor(email => email.Description)
                .MaximumLength(50).WithMessage("Description cannot exceed 50 characters.");

            RuleFor(email => email.RelatedEntityType)
                .NotEmpty().WithMessage("Related entity type is required.")
                .Must(type => new[] { "User", "Company", "Branch", "PropertyOwner", "PropertyTenant", "PropertyBeneficiary", "Vendor" }.Contains(type))
                .WithMessage("Related entity type must be valid.");

            RuleFor(email => email)
                .Must(e => e.RelatedEntityId.HasValue || !string.IsNullOrEmpty(e.RelatedEntityStringId))
                .WithMessage("Related entity ID is required.");
        }
    }

    #endregion

    #region Media

    public class Media
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(255)]
        public string? FileName { get; set; }

        [StringLength(255)]
        public string? FilePath { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public int MediaTypeId { get; set; } // FK to MediaType

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UploadedBy { get; set; }

        // Reference properties
        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; set; }

        // Navigation property
        [ForeignKey("MediaTypeId")]
        public virtual BusinessMappingModels.MediaType? MediaType { get; set; }
    }

    #endregion

    #region Note

    public class Note
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string? Title { get; set; }

        [Required]
        [StringLength(4000)]
        public string? Content { get; set; }

        public int NoteTypeId { get; set; } // FK to NoteType

        public bool IsPrivate { get; set; } = false;

        // Entity relationship fields
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; } // "Property", "PropertyOwner", "Tenant", "Beneficiary", "Vendor", etc.

        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; } // For user IDs which are strings

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation property
        [ForeignKey("NoteTypeId")]
        public virtual NoteType? NoteType { get; set; }
    }

    #endregion

    #region Communication

    public class Communication
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string? Subject { get; set; }

        [Required]
        [StringLength(4000)]
        public string? Content { get; set; }

        public int CommunicationChannelId { get; set; } // FK to CommunicationChannel

        public int CommunicationDirectionId { get; set; } // FK to CommunicationDirection (Inbound/Outbound)

        [StringLength(256)]
        public string? FromEmailAddress { get; set; }

        [StringLength(256)]
        public string? ToEmailAddress { get; set; }

        [StringLength(20)]
        public string? FromPhoneNumber { get; set; }

        [StringLength(20)]
        public string? ToPhoneNumber { get; set; }

        // Entity relationship fields
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; }

        // Related participants 
        public string? RelatedUserId { get; set; } // The system user involved

        public int? RelatedPropertyId { get; set; }

        public int? RelatedOwnerId { get; set; }

        public int? RelatedTenantId { get; set; }

        public int? RelatedBeneficiaryId { get; set; }

        public int? RelatedVendorId { get; set; }

        // Document attachment
        public int? AttachmentId { get; set; } // FK to CdnFileMetadata

        // Audit fields
        public DateTime CommunicationDate { get; set; } = DateTime.Now;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CommunicationChannelId")]
        public virtual CommunicationChannel? CommunicationChannel { get; set; }

        [ForeignKey("CommunicationDirectionId")]
        public virtual CommunicationDirection? CommunicationDirection { get; set; }
    }

    #endregion

    #region Reminder

    public class Reminder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public int ReminderTypeId { get; set; } // FK to ReminderType

        public int ReminderStatusId { get; set; } // FK to ReminderStatus

        public DateTime DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public bool IsRecurring { get; set; } = false;

        public int? RecurrenceFrequencyId { get; set; } // FK to RecurrenceFrequency

        public int? RecurrenceInterval { get; set; } // Every X days/weeks/months

        public DateTime? RecurrenceEndDate { get; set; }

        // Notification settings
        public bool SendNotification { get; set; } = true;

        public int? NotifyDaysBefore { get; set; }

        // Entity relationship fields
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; }

        // Assigned to
        [StringLength(50)]
        public string? AssignedToUserId { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("ReminderTypeId")]
        public virtual ReminderType? ReminderType { get; set; }

        [ForeignKey("ReminderStatusId")]
        public virtual ReminderStatus? ReminderStatus { get; set; }

        [ForeignKey("RecurrenceFrequencyId")]
        public virtual RecurrenceFrequency? RecurrenceFrequency { get; set; }
    }

    #endregion

    #region NotificationPreference

    public class NotificationPreference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Entity identification
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; }

        // Notification event type
        public int NotificationEventTypeId { get; set; } // FK to NotificationEventType

        // Channel preferences
        public bool EmailEnabled { get; set; } = true;

        public bool SmsEnabled { get; set; } = false;

        public bool PushEnabled { get; set; } = false;

        public bool WebEnabled { get; set; } = true;

        // Time preferences
        public bool OnlyDuringBusinessHours { get; set; } = false;

        [StringLength(100)]
        public string? PreferredTimeOfDay { get; set; } // e.g., "Morning", "Afternoon", "Evening"

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation property
        [ForeignKey("NotificationEventTypeId")]
        public virtual NotificationEventType? NotificationEventType { get; set; }
    }

    #endregion

    #region EntityDocument

    public class EntityDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Entity relationship
        [Required]
        [StringLength(20)]
        public string? EntityType { get; set; } // "Property", "PropertyOwner", "Tenant", etc.

        public int EntityId { get; set; }

        // Document type and mapping
        public int DocumentTypeId { get; set; } // FK to DocumentType

        public int? CdnFileMetadataId { get; set; } // FK to CdnFileMetadata

        // Status
        public int DocumentStatusId { get; set; } // FK to DocumentStatus

        public bool IsRequired { get; set; } = false;

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("DocumentTypeId")]
        public virtual DocumentType? DocumentType { get; set; }

        [ForeignKey("DocumentStatusId")]
        public virtual DocumentStatus? DocumentStatus { get; set; }
    }

    #endregion

    #region Response Models

    public class ResponseModel
    {
        public object? Response { get; set; }
        public ResponseInfo ResponseInfo { get; set; } = new ResponseInfo();
    }

    public class ResponseInfo
    {
        public string? Message { get; set; }
        public bool Success { get; set; }
    }

    #endregion

    #region Extensions

    public static class FileNameExtensions
    {
        public static string InsertBeforeExtension(this string fileName, string insert)
        {
            var lastDot = fileName.LastIndexOf('.');
            if (lastDot < 0)
                return fileName + insert;

            return fileName.Substring(0, lastDot) + insert + fileName.Substring(lastDot);
        }
    }

    public static class BlazorSsrRedirectManagerExtensions
    {
        public static void RedirectTo(this HttpContext httpContext, string redirectionUrl)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            httpContext.Response.Headers.Append("blazor-enhanced-nav-redirect-location", redirectionUrl);
            httpContext.Response.StatusCode = 200;
        }
    }

    #endregion

    #region Permission
    public static class PermissionFormat
    {
        public static string GetRoleBadgeClass(SystemRole role)
        {
            return role switch
            {
                SystemRole.SystemAdministrator => "role-admin",
                SystemRole.CompanyAdministrator => "role-company-admin",
                SystemRole.BranchManager => "role-branch-manager",
                SystemRole.PropertyManager => "role-property-manager",
                SystemRole.FinancialOfficer => "role-financial",
                SystemRole.TenantOfficer => "role-tenant",
                SystemRole.ReportsViewer => "role-reports",
                _ => "role-default"
            };
        }

        public static string GetRoleDisplayName(SystemRole role)
        {
            return role switch
            {
                SystemRole.SystemAdministrator => "System Admin",
                SystemRole.CompanyAdministrator => "Company Admin",
                SystemRole.BranchManager => "Branch Manager",
                SystemRole.PropertyManager => "Property Manager",
                SystemRole.FinancialOfficer => "Financial Officer",
                SystemRole.TenantOfficer => "Tenant Officer",
                SystemRole.ReportsViewer => "Reports Viewer",
                _ => "Unknown"
            };
        }

        public static string GetRoleIcon(SystemRole? role)
        {
            return role switch
            {
                SystemRole.SystemAdministrator => "fa-light fa-user-crown",
                SystemRole.CompanyAdministrator => "fa-light fa-user-tie",
                SystemRole.BranchManager => "fa-light fa-user-hard-hat",
                SystemRole.PropertyManager => "fa-light fa-user-chart",
                SystemRole.FinancialOfficer => "fa-light fa-user-dollar",
                SystemRole.TenantOfficer => "fa-light fa-user-headset",
                SystemRole.ReportsViewer => "fa-light fa-user-chart",
                _ => "fa-light fa-user"
            };
        }

        public static string GetRoleIconSmall(SystemRole? role)
        {
            return role switch
            {
                SystemRole.SystemAdministrator => "fa-light fa-shield-check",
                SystemRole.CompanyAdministrator => "fa-light fa-tie",
                SystemRole.BranchManager => "fa-light fa-hard-hat",
                SystemRole.PropertyManager => "fa-light fa-chart-line",
                SystemRole.FinancialOfficer => "fa-light fa-dollar-sign",
                SystemRole.TenantOfficer => "fa-light fa-headset",
                SystemRole.ReportsViewer => "fa-light fa-chart-pie",
                _ => "fa-light fa-user"
            };
        }

        public static string GetRoleDescription(SystemRole? role)
        {
            return role switch
            {
                SystemRole.SystemAdministrator => "Full system access with all permissions",
                SystemRole.CompanyAdministrator => "Administrates all branches within a company",
                SystemRole.BranchManager => "Manages operations for a specific branch",
                SystemRole.PropertyManager => "Manages properties, tenants, and related operations",
                SystemRole.FinancialOfficer => "Manages financial operations, payments and reports",
                SystemRole.TenantOfficer => "Manages tenant relationships and operations",
                SystemRole.ReportsViewer => "View-only access to reports and dashboards",
                _ => "Standard user role with limited permissions"
            };
        }
    }
    #endregion
}