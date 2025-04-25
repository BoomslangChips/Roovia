using Microsoft.AspNetCore.Identity;

namespace Roovia.Models.Users
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public int CompanyId { get; set; }

        public bool IsActive { get; set; }
    }

}
