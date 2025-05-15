using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Models.BusinessHelperModels
{
    #region Address

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

    #endregion

    #region BankAccount

    public class BankAccount
    {
        [StringLength(100)]
        public string? AccountType { get; set; }

        [StringLength(10)]
        public string? AccountNumber { get; set; }

        public BankName? BankName { get; set; }

        [StringLength(6)]
        public string? BranchCode { get; set; }
    }

    public enum BankName
    {
        Absa,
        Capitec,
        FNB,
        Nedbank,
        StandardBank,
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
                .Matches(@"^\d{10}$").WithMessage("Account number must be a valid 10-digit number.");
            RuleFor(account => account.BranchCode)
                .NotEmpty().WithMessage("Branch code is required.")
                .Matches(@"^\d{6}$").WithMessage("Branch code must be a valid 6-digit number.");
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

        public ContactNumberType Type { get; set; } = ContactNumberType.Mobile;

        [StringLength(50)]
        public string? Description { get; set; } // e.g., "Work", "Home", etc.

        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;

        // Entity relationship fields
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; } // "User", "Company", "Branch", "PropertyOwner", "Tenant", "Beneficiary", "Vendor"

        public int? RelatedEntityId { get; set; } // For entities with int IDs

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; } // For entities with string IDs

        // Navigation properties to support relationships (optional)
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

    public enum ContactNumberType
    {
        Mobile,
        Landline,
        Fax,
        WhatsApp,
        Other
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
        public string? Description { get; set; } // e.g., "Work", "Personal", etc.

        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;

        // Entity relationship fields
        [Required]
        [StringLength(20)]
        public string? RelatedEntityType { get; set; } // "User", "Company", "Branch", "PropertyOwner", "Tenant", "Beneficiary", "Vendor"

        public int? RelatedEntityId { get; set; } // For entities with int IDs

        [StringLength(50)]
        public string? RelatedEntityStringId { get; set; } // For entities with string IDs

        // Navigation properties to support relationships (optional)
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
        public int Id { get; set; }

        [StringLength(255)]
        public string? FileName { get; set; }

        [StringLength(255)]
        public string? FilePath { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }
        public MediaType Type { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UploadedBy { get; set; }

        // Reference properties
        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; set; }
    }

    public enum MediaType
    {
        Image,
        Document,
        Video,
        Audio,
        Other
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