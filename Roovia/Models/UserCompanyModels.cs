using Microsoft.AspNetCore.Identity;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessMappingModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.ProjectCdnConfigModels;
using Roovia.Models.UserCompanyMappingModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roovia.Models.UserCompanyModels
{
    #region Companies

    public class Company
    {
        [Key]
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

        // CDN Integration for Main Logo
        public int? MainLogoId { get; set; } // FK to CdnFileMetadata

        // Address (owned entity)
        public Address Address { get; set; } = new Address();

        // Bank details
        public BankAccount BankAccount { get; set; } = new BankAccount();

        // Status
        public int? StatusId { get; set; } // FK to CompanyStatusType

        public bool IsActive { get; set; } = true;

        // Subscription/Plan details
        public int? SubscriptionPlanId { get; set; }

        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public bool IsTrialPeriod { get; set; } = false;

        // Limits
        public int? MaxUsers { get; set; }

        public int? MaxProperties { get; set; }
        public int? MaxBranches { get; set; }

        // Settings
        [StringLength(4000)]
        public string? Settings { get; set; } // JSON configuration

        // Tags for categorization
        [StringLength(500)]
        public string? Tags { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public bool IsRemoved { get; set; }

        public DateTime? RemovedDate { get; set; }

        [StringLength(100)]
        public string? RemovedBy { get; set; }

        // Navigation properties
        [ForeignKey("StatusId")]
        public virtual CompanyStatusType? Status { get; set; }

        [ForeignKey("MainLogoId")]
        public virtual CdnFileMetadata? MainLogo { get; set; }

        [ForeignKey("SubscriptionPlanId")]
        public virtual SubscriptionPlan? SubscriptionPlan { get; set; }

        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Email> EmailAddresses { get; set; } = new List<Email>();
        public virtual ICollection<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();
        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();
        public virtual ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();
        public virtual ICollection<EntityDocumentRequirement> DocumentRequirements { get; set; } = new List<EntityDocumentRequirement>();

        // Helper properties
        [NotMapped]
        public string? PrimaryEmail => EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;

        [NotMapped]
        public string? PrimaryContactNumber => ContactNumbers?.FirstOrDefault(c => c.IsPrimary)?.Number;
    }

    #endregion Companies

    #region Branches

    public class Branch
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Code { get; set; } // Unique branch code

        // CDN Integration for Main Logo
        public int? MainLogoId { get; set; } // FK to CdnFileMetadata

        // Address (owned entity)
        public Address Address { get; set; } = new Address();

        // Bank details
        public BankAccount BankAccount { get; set; } = new BankAccount();

        // Status
        public int? StatusId { get; set; } // FK to BranchStatusType

        public bool IsActive { get; set; } = true;

        public bool IsHeadOffice { get; set; } = false;

        // Settings
        [StringLength(1000)]
        public string? Settings { get; set; } // JSON configuration

        // Limits
        public int? MaxUsers { get; set; }

        public int? MaxProperties { get; set; }

        // Tags for categorization
        [StringLength(500)]
        public string? Tags { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public bool IsRemoved { get; set; }

        public DateTime? RemovedDate { get; set; }

        [StringLength(100)]
        public string? RemovedBy { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("StatusId")]
        public virtual BranchStatusType? Status { get; set; }

        [ForeignKey("MainLogoId")]
        public virtual CdnFileMetadata? MainLogo { get; set; }

        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Email> EmailAddresses { get; set; } = new List<Email>();
        public virtual ICollection<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();
        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();
        public virtual ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();

        // Helper properties
        [NotMapped]
        public string? PrimaryEmail => EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;

        [NotMapped]
        public string? PrimaryContactNumber => ContactNumbers?.FirstOrDefault(c => c.IsPrimary)?.Number;
    }

    #endregion Branches

    #region Users

    public class ApplicationUser : IdentityUser
    {
        // Override the base Email property to handle it through our custom Email entity
        public override string? Email { get => GetPrimaryEmail(); set => SetPrimaryEmail(value); }

        public override string? PhoneNumber { get => GetPrimaryPhoneNumber(); set => SetPrimaryPhoneNumber(value); }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? IdNumber { get; set; }

        public int? CompanyId { get; set; }

        public int? BranchId { get; set; }

        // Profile picture - CDN Integration
        public int? ProfilePictureId { get; set; } // FK to CdnFileMetadata

        // Employment details
        [StringLength(100)]
        public string? EmployeeNumber { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        public DateTime? HireDate { get; set; }

        // System role
        public SystemRole? Role { get; set; }

        // Status
        public int? StatusId { get; set; } // FK to UserStatusType

        public bool IsActive { get; set; } = true;

        public bool RequireChangePasswordOnLogin { get; set; }

        // Settings
        [StringLength(4000)]
        public string? UserPreferences { get; set; } // JSON configuration

        // Two-factor settings
        public bool IsTwoFactorRequired { get; set; } = false;

        public string? PreferredTwoFactorMethod { get; set; }

        // Login tracking
        public DateTime? LastLoginDate { get; set; }

        public int LoginFailureCount { get; set; } = 0;
        public DateTime? LastLoginFailureDate { get; set; }
        public string? LastLoginIpAddress { get; set; }

        // Tags for categorization
        [StringLength(500)]
        public string? Tags { get; set; }

        // Audit fields
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public bool IsRemoved { get; set; }

        public DateTime? RemovedDate { get; set; }

        [StringLength(100)]
        public string? RemovedBy { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        [ForeignKey("StatusId")]
        public virtual UserStatusType? Status { get; set; }

        [ForeignKey("ProfilePictureId")]
        public virtual CdnFileMetadata? ProfilePicture { get; set; }

        public virtual ICollection<Email> EmailAddresses { get; set; } = new List<Email>();
        public virtual ICollection<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();
        public virtual ICollection<UserRoleAssignment> CustomRoles { get; set; } = new List<UserRoleAssignment>();
        public virtual ICollection<UserPermissionOverride> PermissionOverrides { get; set; } = new List<UserPermissionOverride>();
        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();

        // Helper property for display
        [NotMapped]
        public string? FullName => $"{FirstName} {LastName}";

        // Helper method to check system role permissions
        public bool HasSystemRole(SystemRole minimumRequiredRole)
        {
            return Role <= minimumRequiredRole;
        }

        // Helper to check custom role permissions
        public async Task<bool> HasPermission(string permissionName, IPermissionService permissionService)
        {
            return await permissionService.UserHasPermission(Id, permissionName);
        }

        // Helper methods for Email compatibility
        private string GetPrimaryEmail()
        {
            var primaryEmail = EmailAddresses?.FirstOrDefault(e => e.IsPrimary);
            return primaryEmail?.EmailAddress ?? base.Email ?? string.Empty;
        }

        private void SetPrimaryEmail(string? value)
        {
            base.Email = value; // Keep the base property updated for Identity

            var primaryEmail = EmailAddresses?.FirstOrDefault(e => e.IsPrimary);
            if (primaryEmail != null)
            {
                primaryEmail.EmailAddress = value;
            }
            else if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(Id))
            {
                var email = new Email
                {
                    EmailAddress = value,
                    IsPrimary = true,
                    IsActive = true,
                    RelatedEntityType = "User",
                    RelatedEntityStringId = Id,
                    CreatedOn = DateTime.Now
                };

                EmailAddresses?.Add(email);
            }
        }

        // Helper methods for PhoneNumber compatibility
        private string GetPrimaryPhoneNumber()
        {
            var primaryPhone = ContactNumbers?.FirstOrDefault(c => c.IsPrimary);
            if (primaryPhone != null)
                return primaryPhone?.Number ?? base.PhoneNumber ?? string.Empty;
            else return string.Empty;
        }

        private void SetPrimaryPhoneNumber(string? value)
        {
            base.PhoneNumber = value; // Keep the base property updated for Identity

            var primaryPhone = ContactNumbers?.FirstOrDefault(c => c.IsPrimary);
            if (primaryPhone != null)
            {
                primaryPhone.Number = value;
            }
            else if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(Id))
            {
                var phone = new ContactNumber
                {
                    Number = value,
                    IsPrimary = true,
                    ContactNumberTypeId = 1, // Assuming 1 is Mobile
                    IsActive = true,
                    RelatedEntityType = "User",
                    RelatedEntityStringId = Id,
                    CreatedOn = DateTime.Now
                };

                ContactNumbers?.Add(phone);
            }
        }
    }

    public enum SystemRole
    {
        SystemAdministrator = 1,      // Full system access with all permissions
        PropertyManager = 2,          // Manages properties, tenants, and related operations
        FinancialOfficer = 3,         // Manages financial operations, payments and reports
        TenantOfficer = 4,            // Manages tenant relationships and operations
        ReportsViewer = 5,            // View-only access to reports and dashboards
        BranchManager = 6,            // Manages operations for a specific branch
        CompanyAdministrator = 7      // Administrates all branches within a company
    }

    #endregion Users

    #region Permissions and Roles

    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string? Name { get; set; }

        [Required, MaxLength(250)]
        public string? Description { get; set; }

        [Required, MaxLength(50)]
        public string? Category { get; set; }

        [Required, MaxLength(100)]
        public string? SystemName { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public virtual ICollection<UserPermissionOverride> UserPermissionOverrides { get; set; } = new List<UserPermissionOverride>();
    }

    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string? Name { get; set; }

        [Required, MaxLength(250)]
        public string? Description { get; set; }

        public int? CompanyId { get; set; } // Null for system roles, populated for company-specific roles

        public int? BranchId { get; set; } // Null for system/company roles, populated for branch-specific roles

        public bool IsSystemRole { get; set; } = false;

        public bool IsPreset { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public virtual ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
    }

    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        public int RoleId { get; set; }

        public int PermissionId { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        [ForeignKey("PermissionId")]
        public virtual Permission? Permission { get; set; }
    }

    public class UserRoleAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        public int RoleId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(100)]
        public string? AssignedBy { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }
    }

    public class UserPermissionOverride
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [Required]
        public int PermissionId { get; set; }

        [Required]
        public bool IsGranted { get; set; }  // true = explicitly grant, false = explicitly deny

        public string? Reason { get; set; } // Reason for override

        public DateTime? ExpiryDate { get; set; }

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("PermissionId")]
        public virtual Permission? Permission { get; set; }
    }

    #endregion Permissions and Roles
}