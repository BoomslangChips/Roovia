using Microsoft.AspNetCore.Identity;

namespace Roovia.Models.Users
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public UserRole? Role { get; set; } = UserRole.StandardUser;
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public Company? Company { get; set; }
        public Branch? Branch { get; set; }

        // Helper property for display
        public string? FullName => $"{FirstName} {LastName}";

        // Helper method to check role permissions
        public bool HasPermission(UserRole minimumRequiredRole)
        {
            return Role <= minimumRequiredRole;
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
