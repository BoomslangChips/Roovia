using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Roovia.Models.Helper;

namespace Roovia.Models.Users
{
    public class Branch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        public Address Address { get; set; } = new Address();
        public bool IsActive { get; set; } = true;
        public int CompanyId { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(450)]
        public string? UpdatedBy { get; set; }

        // Logo properties
        [StringLength(255)]
        public string? LogoPath { get; set; }

        [StringLength(255)]
        public string? LogoSmallPath { get; set; } // For login screens

        [StringLength(255)]
        public string? LogoMediumPath { get; set; } // For general use

        [StringLength(255)]
        public string? LogoLargePath { get; set; } // For invoices or high-resolution needs

        // Navigation properties
        public Company? Company { get; set; }
        public List<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public List<BranchLogo> Logos { get; set; } = new List<BranchLogo>();
        public List<Email> EmailAddresses { get; set; } = new List<Email>();
        public List<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();

        // Helper methods to get primary contact info
        public string? GetPrimaryEmail() => EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
        public string? GetPrimaryContactNumber() => ContactNumbers?.FirstOrDefault(c => c.IsPrimary)?.Number;
    }
}