using Microsoft.AspNetCore.Identity;
using Roovia.Models.Helper;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        public UserRole? Role { get; set; } = UserRole.StandardUser;
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

        // Helper property for display
        [StringLength(101)]
        public string? FullName => $"{FirstName} {LastName}";

        // Helper method to check role permissions
        public bool HasPermission(UserRole minimumRequiredRole)
        {
            return Role <= minimumRequiredRole;
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

                EmailAddresses.Add(email);
            }
        }

        // Helper methods for PhoneNumber compatibility
        private string GetPrimaryPhoneNumber()
        {
            var primaryPhone = ContactNumbers?.FirstOrDefault(c => c.IsPrimary);
            return primaryPhone?.Number ?? base.PhoneNumber;
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

                ContactNumbers.Add(phone);
            }
        }
    }

    public enum UserRole
    {
        GlobalAdmin = 1,
        CompanyAdmin = 2,
        BranchManager = 3,
        StandardUser = 4
    }
}