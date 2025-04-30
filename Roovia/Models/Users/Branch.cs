using System;
using System.Collections.Generic;
using Roovia.Models.Helper;

namespace Roovia.Models.Users
{
    public class Branch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public Address Address { get; set; } = new Address();
        public bool IsActive { get; set; } = true;
        public Guid CompanyId { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public List<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

        // Navigation property
        public Company? Company { get; set; }
    }
}