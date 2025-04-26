using Microsoft.AspNetCore.Identity;

namespace Roovia.Models.Users
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public Guid? CompanyId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
        public bool IsActive { get; set; }
    }

}
