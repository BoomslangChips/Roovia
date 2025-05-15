using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roovia.Services
{
    public class VendorService : IVendor
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<VendorService> _logger;

        public VendorService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<VendorService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<ResponseModel> CreateVendor(Vendor vendor, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the company exists
                var company = await context.Companies
                    .FirstOrDefaultAsync(c => c.Id == companyId && !c.IsRemoved);

                if (company == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Company not found.";
                    return response;
                }

                // Set audit fields
                vendor.CompanyId = companyId;
                vendor.CreatedOn = DateTime.Now;

                // Add the vendor
                await context.Vendors.AddAsync(vendor);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdVendor = await GetVendorWithDetails(context, vendor.Id);

                response.Response = createdVendor;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendor created successfully.";

                _logger.LogInformation("Vendor created with ID: {VendorId} for company {CompanyId}", 
                    vendor.Id, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the vendor: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetVendorById(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .Include(v => v.EmailAddresses.Where(e => e.IsActive))
                    .Include(v => v.ContactNumbers.Where(c => c.IsActive))
                    .Include(v => v.MaintenanceTickets)
                        .ThenInclude(mt => mt.Property)
                    .Include(v => v.MaintenanceTickets)
                        .ThenInclude(mt => mt.Status)
                    .Where(v => v.Id == id && v.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (vendor != null)
                {
                    response.Response = vendor;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Vendor retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendor {VendorId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the vendor: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateVendor(int id, Vendor updatedVendor, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .Include(v => v.EmailAddresses)
                    .Include(v => v.ContactNumbers)
                    .FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                // Update vendor fields
                vendor.Name = updatedVendor.Name;
                vendor.ContactPerson = updatedVendor.ContactPerson;
                vendor.Address = updatedVendor.Address;
                vendor.Specializations = updatedVendor.Specializations;
                vendor.IsPreferred = updatedVendor.IsPreferred;
                vendor.IsActive = updatedVendor.IsActive;
                vendor.BankAccount = updatedVendor.BankAccount;
                vendor.HasInsurance = updatedVendor.HasInsurance;
                vendor.InsurancePolicyNumber = updatedVendor.InsurancePolicyNumber;
                vendor.InsuranceExpiryDate = updatedVendor.InsuranceExpiryDate;

                await context.SaveChangesAsync();

                // Reload with related data
                var updatedResult = await GetVendorWithDetails(context, id);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendor updated successfully.";

                _logger.LogInformation("Vendor updated: {VendorId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor {VendorId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the vendor: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteVendor(int id, int companyId, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .Include(v => v.MaintenanceTickets)
                    .FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                // Check if vendor has active maintenance tickets
                if (vendor.MaintenanceTickets.Any(mt => mt.StatusId == 1 || mt.StatusId == 2))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete vendor with active maintenance tickets.";
                    return response;
                }

                // Soft delete - mark as inactive
                vendor.IsActive = false;

                await context.SaveChangesAsync();

                response.Response = vendor;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendor deleted successfully.";

                _logger.LogInformation("Vendor soft deleted: {VendorId} by {UserId}", id, user?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor {VendorId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the vendor: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllVendors(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendors = await context.Vendors
                    .Include(v => v.EmailAddresses.Where(e => e.IsActive))
                    .Include(v => v.ContactNumbers.Where(c => c.IsActive))
                    .Where(v => v.CompanyId == companyId && v.IsActive)
                    .OrderBy(v => v.Name)
                    .ToListAsync();

                response.Response = vendors;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendors retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} vendors for company {CompanyId}", vendors.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendors for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving vendors: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetActiveVendors(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendors = await context.Vendors
                    .Include(v => v.EmailAddresses.Where(e => e.IsActive))
                    .Include(v => v.ContactNumbers.Where(c => c.IsActive))
                    .Where(v => v.CompanyId == companyId && v.IsActive && v.HasInsurance)
                    .OrderBy(v => v.IsPreferred ? 0 : 1)
                    .ThenBy(v => v.Rating ?? 0)
                    .ThenBy(v => v.Name)
                    .ToListAsync();

                response.Response = vendors;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Active vendors retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} active vendors for company {CompanyId}", vendors.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active vendors for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving vendors: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetVendorsBySpecialization(int companyId, string specialization)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendors = await context.Vendors
                    .Include(v => v.EmailAddresses.Where(e => e.IsActive))
                    .Include(v => v.ContactNumbers.Where(c => c.IsActive))
                    .Where(v => v.CompanyId == companyId && 
                               v.IsActive && 
                               v.Specializations != null &&
                               v.Specializations.ToLower().Contains(specialization.ToLower()))
                    .OrderBy(v => v.Rating ?? 0)
                    .ThenBy(v => v.Name)
                    .ToListAsync();

                response.Response = vendors;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Vendors with specialization '{specialization}' retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} vendors with specialization {Specialization} for company {CompanyId}", 
                    vendors.Count, specialization, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendors by specialization for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving vendors: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddEmailAddress(int vendorId, Email email)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .Include(v => v.EmailAddresses)
                    .FirstOrDefaultAsync(v => v.Id == vendorId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                // If marking as primary, unset other primary emails
                if (email.IsPrimary)
                {
                    foreach (var existingEmail in vendor.EmailAddresses.Where(e => e.IsPrimary))
                    {
                        existingEmail.IsPrimary = false;
                        existingEmail.UpdatedDate = DateTime.Now;
                    }
                }

                // Set relation properties
                email.RelatedEntityType = "Vendor";
                email.RelatedEntityId = vendorId;
                email.VendorId = vendorId;
                email.CreatedOn = DateTime.Now;
                email.CreatedBy = vendor.CreatedBy;

                vendor.EmailAddresses.Add(email);
                await context.SaveChangesAsync();

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email address for vendor {VendorId}", vendorId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddContactNumber(int vendorId, ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .Include(v => v.ContactNumbers)
                    .FirstOrDefaultAsync(v => v.Id == vendorId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                // If marking as primary, unset other primary numbers
                if (contactNumber.IsPrimary)
                {
                    foreach (var existingNumber in vendor.ContactNumbers.Where(c => c.IsPrimary))
                    {
                        existingNumber.IsPrimary = false;
                        existingNumber.UpdatedDate = DateTime.Now;
                    }
                }

                // Set relation properties
                contactNumber.RelatedEntityType = "Vendor";
                contactNumber.RelatedEntityId = vendorId;
                contactNumber.VendorId = vendorId;
                contactNumber.CreatedOn = DateTime.Now;
                contactNumber.CreatedBy = vendor.CreatedBy;

                vendor.ContactNumbers.Add(contactNumber);
                await context.SaveChangesAsync();

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact number for vendor {VendorId}", vendorId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the contact number: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateVendorRating(int vendorId, decimal rating, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .Include(v => v.MaintenanceTickets)
                    .FirstOrDefaultAsync(v => v.Id == vendorId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                // Calculate average rating based on completed tickets
                var completedTickets = vendor.MaintenanceTickets
                    .Where(mt => mt.StatusId == 4) // Completed
                    .Count();

                if (vendor.Rating.HasValue && vendor.TotalJobs.HasValue && vendor.TotalJobs > 0)
                {
                    var totalRating = vendor.Rating.Value * vendor.TotalJobs.Value;
                    vendor.Rating = (totalRating + rating) / (vendor.TotalJobs.Value + 1);
                    vendor.TotalJobs++;
                }
                else
                {
                    vendor.Rating = rating;
                    vendor.TotalJobs = 1;
                }

                await context.SaveChangesAsync();

                response.Response = new { VendorId = vendorId, NewRating = vendor.Rating, TotalJobs = vendor.TotalJobs };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendor rating updated successfully.";

                _logger.LogInformation("Vendor rating updated: {VendorId} to {Rating} by {UserId}", 
                    vendorId, vendor.Rating, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor rating {VendorId}", vendorId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating vendor rating: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> SetPreferredVendor(int vendorId, bool isPreferred, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .FirstOrDefaultAsync(v => v.Id == vendorId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                vendor.IsPreferred = isPreferred;

                await context.SaveChangesAsync();

                response.Response = vendor;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Vendor {(isPreferred ? "marked as" : "removed from")} preferred list.";

                _logger.LogInformation("Vendor preferred status updated: {VendorId} to {IsPreferred} by {UserId}", 
                    vendorId, isPreferred, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor preferred status {VendorId}", vendorId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating vendor status: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetVendorPerformanceStats(int vendorId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .Include(v => v.MaintenanceTickets)
                        .ThenInclude(mt => mt.Expenses)
                    .FirstOrDefaultAsync(v => v.Id == vendorId && v.CompanyId == companyId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                var tickets = vendor.MaintenanceTickets;

                var statistics = new
                {
                    VendorName = vendor.Name,
                    Rating = vendor.Rating ?? 0,
                    TotalJobs = vendor.TotalJobs ?? 0,
                    CompletedJobs = tickets.Count(t => t.StatusId == 4),
                    ActiveJobs = tickets.Count(t => t.StatusId == 2),
                    IssuesResolved = tickets.Count(t => t.IssueResolved == true),
                    TotalRevenue = tickets.SelectMany(t => t.Expenses)
                        .Where(e => e.VendorId == vendorId)
                        .Sum(e => e.Amount),
                    AverageJobCost = tickets.Any() ? 
                        tickets.Where(t => t.ActualCost.HasValue)
                            .Average(t => t.ActualCost.Value) : 0,
                    AverageCompletionTime = CalculateAverageCompletionTime(tickets),
                    JobsByCategory = tickets.GroupBy(t => t.CategoryId)
                        .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                        .ToList(),
                    MonthlyTrend = CalculateMonthlyTrend(tickets)
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendor performance statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendor performance stats for vendor {VendorId}", vendorId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving statistics: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetVendorsWithExpiredInsurance(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendors = await context.Vendors
                    .Include(v => v.EmailAddresses.Where(e => e.IsActive))
                    .Include(v => v.ContactNumbers.Where(c => c.IsActive))
                    .Where(v => v.CompanyId == companyId && 
                               v.IsActive &&
                               v.HasInsurance &&
                               v.InsuranceExpiryDate.HasValue &&
                               v.InsuranceExpiryDate.Value <= DateTime.Now)
                    .OrderBy(v => v.InsuranceExpiryDate)
                    .ToListAsync();

                response.Response = vendors;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendors with expired insurance retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} vendors with expired insurance for company {CompanyId}", 
                    vendors.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendors with expired insurance for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving vendors: " + ex.Message;
            }

            return response;
        }

        // Helper methods
        private async Task<Vendor> GetVendorWithDetails(ApplicationDbContext context, int vendorId)
        {
            return await context.Vendors
                .Include(v => v.EmailAddresses.Where(e => e.IsActive))
                .Include(v => v.ContactNumbers.Where(c => c.IsActive))
                .Include(v => v.MaintenanceTickets)
                    .ThenInclude(mt => mt.Property)
                .Include(v => v.MaintenanceTickets)
                    .ThenInclude(mt => mt.Status)
                .FirstOrDefaultAsync(v => v.Id == vendorId);
        }

        private double CalculateAverageCompletionTime(ICollection<MaintenanceTicket> tickets)
        {
            var completedTickets = tickets
                .Where(t => t.StatusId == 4 && t.CompletedDate.HasValue)
                .ToList();

            if (!completedTickets.Any())
                return 0;

            var totalDays = completedTickets
                .Sum(t => (t.CompletedDate.Value - t.CreatedOn).TotalDays);

            return Math.Round(totalDays / completedTickets.Count, 1);
        }

        private object CalculateMonthlyTrend(ICollection<MaintenanceTicket> tickets)
        {
            var monthlyData = tickets
                .Where(t => t.CreatedOn >= DateTime.Now.AddMonths(-6))
                .GroupBy(t => new { t.CreatedOn.Year, t.CreatedOn.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalJobs = g.Count(),
                    CompletedJobs = g.Count(t => t.StatusId == 4),
                    TotalRevenue = g.SelectMany(t => t.Expenses)
                        .Sum(e => e.Amount)
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            return monthlyData;
        }
    }
}