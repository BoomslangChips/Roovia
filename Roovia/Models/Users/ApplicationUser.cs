using Microsoft.AspNetCore.Identity;
using Roovia.Interfaces;
using Roovia.Models.Helper;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roovia.Models.Users
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        // Override the base Email property to handle it through our custom Email entity
        public override string Email { get => GetPrimaryEmail(); set => SetPrimaryEmail(value); }
        public override string PhoneNumber { get => GetPrimaryPhoneNumber(); set => SetPrimaryPhoneNumber(value); }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }
        public SystemRole? Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public Company? Company { get; set; }
        public Branch? Branch { get; set; }
        public List<Email> EmailAddresses { get; set; } = new List<Email>();
        public List<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();
        public virtual ICollection<UserRoleAssignment> CustomRoles { get; set; } = new List<UserRoleAssignment>();

        public bool RequireChangePasswordOnLogin { get; set; }

        // Helper property for display
        [StringLength(101)]
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
            return primaryEmail?.EmailAddress ?? base.Email;
        }

        private void SetPrimaryEmail(string value)
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
                return primaryPhone?.Number ?? base.PhoneNumber;
            else return string.Empty;
        }

        private void SetPrimaryPhoneNumber(string value)
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
                    Type = ContactNumberType.Mobile,
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
        GlobalAdmin = 0,        // System Administrator
        PropertyManager = 1,       // Property Manager (this is the default role)
        FinancialOfficer = 2,
        TenantOfficer = 3,
        ReportsViewer = 4,        
        BranchManager = 5,      // Branch Manager
        CompanyAdmin = 6        // Company Administrator
    }

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

        public bool IsActive { get; set; } = true;

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string? Name { get; set; }

        [Required, MaxLength(250)]
        public string? Description { get; set; }

        public bool IsPreset { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

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

        [MaxLength(100)]
        public string? AssignedBy { get; set; }

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

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        [ForeignKey("PermissionId")]
        public virtual Permission? Permission { get; set; }
    }
}