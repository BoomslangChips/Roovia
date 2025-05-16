using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Services.General
{
    /// <summary>
    /// Service responsible for managing vendors and their related operations
    /// </summary>
    public class VendorService : IVendor
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<VendorService> _logger;
        private readonly IEmailService _emailService;
        private readonly ICdnService _cdnService;

        public VendorService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<VendorService> logger,
            IEmailService emailService,
            ICdnService cdnService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _emailService = emailService;
            _cdnService = cdnService;
        }

        #region CRUD Operations

        /// <summary>
        /// Creates a new vendor for a company
        /// </summary>
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

                // Send notification email if vendor has email address
                if (vendor.EmailAddresses != null && vendor.EmailAddresses.Any(e => e.IsPrimary))
                {
                    try
                    {
                        await _emailService.SendVendorWelcomeEmailAsync(vendor);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send welcome email to vendor {VendorId}", vendor.Id);
                        // Continue despite email failure
                    }
                }

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

        /// <summary>
        /// Retrieves a vendor by ID for a specific company
        /// </summary>
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
                    .Include(v => v.BankAccount.BankName)
                    .Include(v => v.Documents)
                        .ThenInclude(d => d.DocumentType)
                    .Include(v => v.Notes)
                        .OrderByDescending(n => n.CreatedOn)
                        .Take(5)
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

        /// <summary>
        /// Updates a vendor's information
        /// </summary>
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

                bool insuranceStatusChanged = vendor.HasInsurance != updatedVendor.HasInsurance ||
                                             (vendor.InsuranceExpiryDate != updatedVendor.InsuranceExpiryDate &&
                                             updatedVendor.InsuranceExpiryDate.HasValue);

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

                // Check if insurance status changed and notify if necessary
                if (insuranceStatusChanged && updatedVendor.HasInsurance)
                {
                    try
                    {
                        await NotifyInsuranceStatusUpdate(vendor);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send insurance update notification for vendor {VendorId}", vendor.Id);
                        // Continue despite email failure
                    }
                }

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

        /// <summary>
        /// Soft deletes a vendor
        /// </summary>
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
                if (vendor.MaintenanceTickets.Any(mt => mt.StatusId is 1 or 2))
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

        #endregion CRUD Operations

        #region Vendor Listing Methods

        /// <summary>
        /// Retrieves all vendors for a company
        /// </summary>
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

        /// <summary>
        /// Retrieves active vendors for a company, sorted by preferred status and rating
        /// </summary>
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
                    .ThenByDescending(v => v.Rating ?? 0)
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

        /// <summary>
        /// Retrieves vendors by specialization
        /// </summary>
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
                    .OrderBy(v => v.IsPreferred ? 0 : 1)
                    .ThenByDescending(v => v.Rating ?? 0)
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

        /// <summary>
        /// Retrieves vendors with expired insurance
        /// </summary>
        public async Task<ResponseModel> GetVendorsWithExpiredInsurance(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var today = DateTime.Today;
                var threeMonthsFromNow = today.AddMonths(3);

                var vendors = await context.Vendors
                    .Include(v => v.EmailAddresses.Where(e => e.IsActive))
                    .Include(v => v.ContactNumbers.Where(c => c.IsActive))
                    .Where(v => v.CompanyId == companyId &&
                               v.IsActive &&
                               v.HasInsurance &&
                               v.InsuranceExpiryDate.HasValue &&
                               (v.InsuranceExpiryDate.Value <= today || v.InsuranceExpiryDate.Value <= threeMonthsFromNow))
                    .OrderBy(v => v.InsuranceExpiryDate)
                    .ToListAsync();

                var expiredVendors = vendors.Where(v => v.InsuranceExpiryDate <= today).ToList();
                var expiringVendors = vendors.Where(v => v.InsuranceExpiryDate > today && v.InsuranceExpiryDate <= threeMonthsFromNow).ToList();

                response.Response = new
                {
                    ExpiredInsurance = expiredVendors,
                    ExpiringInsurance = expiringVendors
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendors with expired/expiring insurance retrieved successfully.";

                _logger.LogInformation("Retrieved {ExpiredCount} vendors with expired insurance and {ExpiringCount} with expiring insurance for company {CompanyId}",
                    expiredVendors.Count, expiringVendors.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendors with expired insurance for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving vendors: " + ex.Message;
            }

            return response;
        }

        #endregion Vendor Listing Methods

        #region Contact Management

        /// <summary>
        /// Adds an email address to a vendor
        /// </summary>
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

        /// <summary>
        /// Adds a contact number to a vendor
        /// </summary>
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

        #endregion Contact Management

        #region Vendor Management

        /// <summary>
        /// Updates a vendor's rating
        /// </summary>
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
                    .Count(mt => mt.StatusId == 4); // Completed

                if (vendor.Rating.HasValue && vendor.TotalJobs.HasValue && vendor.TotalJobs > 0)
                {
                    var totalRating = vendor.Rating.Value * vendor.TotalJobs.Value;
                    vendor.Rating = Math.Round((totalRating + rating) / (vendor.TotalJobs.Value + 1), 2);
                    vendor.TotalJobs++;
                }
                else
                {
                    vendor.Rating = rating;
                    vendor.TotalJobs = 1;
                }

                // If rating is high (above 4.0) and vendor isn't already preferred, suggest making them preferred
                bool suggestPreferred = rating >= 4.0m && !vendor.IsPreferred && (vendor.TotalJobs >= 5);

                await context.SaveChangesAsync();

                response.Response = new
                {
                    VendorId = vendorId,
                    NewRating = vendor.Rating,
                    TotalJobs = vendor.TotalJobs,
                    SuggestPreferred = suggestPreferred
                };

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

        /// <summary>
        /// Sets or unsets preferred status for a vendor
        /// </summary>
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

        /// <summary>
        /// Gets performance statistics for a vendor
        /// </summary>
        public async Task<ResponseModel> GetVendorPerformanceStats(int vendorId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .Include(v => v.MaintenanceTickets)
                        .ThenInclude(mt => mt.Expenses)
                    .Include(v => v.MaintenanceTickets)
                        .ThenInclude(mt => mt.Status)
                    .Include(v => v.MaintenanceTickets)
                        .ThenInclude(mt => mt.Category)
                    .FirstOrDefaultAsync(v => v.Id == vendorId && v.CompanyId == companyId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                var tickets = vendor.MaintenanceTickets;
                var completedTickets = tickets.Where(t => t.StatusId == 4).ToList();
                var onTimeCompletions = completedTickets.Count(t =>
                    t.CompletedDate.HasValue &&
                    t.ScheduledDate.HasValue &&
                    t.CompletedDate.Value <= t.ScheduledDate.Value.AddDays(1));

                decimal onTimeRate = completedTickets.Count > 0
                    ? Math.Round((decimal)onTimeCompletions / completedTickets.Count * 100, 1)
                    : 0;

                var statistics = new
                {
                    VendorName = vendor.Name,
                    VendorHasInsurance = vendor.HasInsurance,
                    InsuranceExpiryDate = vendor.InsuranceExpiryDate,
                    IsInsuranceExpired = vendor.InsuranceExpiryDate.HasValue && vendor.InsuranceExpiryDate.Value < DateTime.Today,
                    IsPreferred = vendor.IsPreferred,
                    Rating = vendor.Rating ?? 0,
                    TotalJobs = vendor.TotalJobs ?? 0,
                    CompletedJobs = tickets.Count(t => t.StatusId == 4),
                    ActiveJobs = tickets.Count(t => t.StatusId == 2),
                    PendingJobs = tickets.Count(t => t.StatusId == 1),
                    IssuesResolved = tickets.Count(t => t.IssueResolved == true),
                    OnTimeCompletionRate = onTimeRate,
                    TotalRevenue = tickets.SelectMany(t => t.Expenses)
                        .Where(e => e.VendorId == vendorId)
                        .Sum(e => e.Amount),
                    AverageJobCost = tickets.Any() ?
                        tickets.Where(t => t.ActualCost.HasValue)
                            .Average(t => t.ActualCost.Value) : 0,
                    AverageCompletionTime = CalculateAverageCompletionTime(tickets),
                    JobsByCategory = tickets
                        .GroupBy(t => t.CategoryId)
                        .Select(g => new
                        {
                            CategoryId = g.Key,
                            CategoryName = g.First().Category?.Name ?? "Unknown",
                            Count = g.Count(),
                            CompletedCount = g.Count(t => t.StatusId == 4)
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    JobsByMonth = CalculateJobsByMonth(tickets),
                    RecentTickets = tickets
                        .OrderByDescending(t => t.CreatedOn)
                        .Take(5)
                        .Select(t => new
                        {
                            TicketId = t.Id,
                            TicketNumber = t.TicketNumber,
                            Title = t.Title,
                            Status = t.Status?.Name,
                            Property = t.Property?.PropertyName,
                            CreatedDate = t.CreatedOn,
                            CompletedDate = t.CompletedDate
                        })
                        .ToList()
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

        #endregion Vendor Management

        #region Documents

        /// <summary>
        /// Uploads an insurance document for a vendor
        /// </summary>
        public async Task<ResponseModel> UploadInsuranceDocument(int vendorId, IFormFile file, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors.FindAsync(vendorId);
                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                // Upload file to CDN
                using var stream = file.OpenReadStream();
                var cdnUrl = await _cdnService.UploadFileAsync(stream, file.FileName, file.ContentType, "documents", $"vendors/{vendorId}/insurance");

                // Create document entry
                var documentTypeId = await context.DocumentTypes
                    .Where(dt => dt.Name == "Insurance Certificate" || dt.Name == "Insurance")
                    .Select(dt => dt.Id)
                    .FirstOrDefaultAsync();

                if (documentTypeId == 0)
                {
                    // Default to 1 if not found
                    documentTypeId = 1;
                }

                var document = new EntityDocument
                {
                    EntityType = "Vendor",
                    EntityId = vendorId,
                    DocumentTypeId = documentTypeId,
                    DocumentStatusId = 1, // Assuming 1 is "Active"
                    Notes = $"Insurance document uploaded on {DateTime.Now:d}",
                    CreatedOn = DateTime.Now,
                    CreatedBy = userId
                };

                await context.EntityDocuments.AddAsync(document);

                // Update vendor insurance info if not already set
                vendor.HasInsurance = true;
                if (!vendor.InsuranceExpiryDate.HasValue)
                {
                    // Default expiry to 1 year from now if not specified
                    vendor.InsuranceExpiryDate = DateTime.Today.AddYears(1);
                }

                await context.SaveChangesAsync();

                response.Response = new { Document = document, CdnUrl = cdnUrl };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Insurance document uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading insurance document for vendor {VendorId}", vendorId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the document: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets documents for a vendor
        /// </summary>
        public async Task<ResponseModel> GetVendorDocuments(int vendorId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var vendor = await context.Vendors
                    .FirstOrDefaultAsync(v => v.Id == vendorId && v.CompanyId == companyId);

                if (vendor == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Vendor not found.";
                    return response;
                }

                var documents = await context.EntityDocuments
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentStatus)
                    .Where(d => d.EntityType == "Vendor" && d.EntityId == vendorId)
                    .OrderByDescending(d => d.CreatedOn)
                    .ToListAsync();

                response.Response = documents;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendor documents retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for vendor {VendorId}", vendorId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the documents: " + ex.Message;
            }

            return response;
        }

        #endregion Documents

        #region Helper Methods

        /// <summary>
        /// Retrieves a vendor with all details
        /// </summary>
        private async Task<Vendor> GetVendorWithDetails(ApplicationDbContext context, int vendorId)
        {
            return await context.Vendors
                .Include(v => v.EmailAddresses.Where(e => e.IsActive))
                .Include(v => v.ContactNumbers.Where(c => c.IsActive))
                .Include(v => v.MaintenanceTickets)
                    .ThenInclude(mt => mt.Property)
                .Include(v => v.MaintenanceTickets)
                    .ThenInclude(mt => mt.Status)
                .Include(v => v.BankAccount.BankName)
                .FirstOrDefaultAsync(v => v.Id == vendorId);
        }

        /// <summary>
        /// Calculates the average completion time for maintenance tickets
        /// </summary>
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

        /// <summary>
        /// Gets job statistics by month
        /// </summary>
        private object CalculateJobsByMonth(ICollection<MaintenanceTicket> tickets)
        {
            var endDate = DateTime.Today;
            var startDate = endDate.AddMonths(-11);

            // Generate all months in the range to ensure complete data even for months with no jobs
            var allMonths = Enumerable.Range(0, 12)
                .Select(i => startDate.AddMonths(i))
                .Select(date => new { Year = date.Year, Month = date.Month, MonthName = date.ToString("MMM") })
                .ToList();

            var ticketsByMonth = tickets
                .Where(t => t.CreatedOn >= startDate)
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
                .ToList();

            // Join with all months to get complete data
            var result = allMonths
                .GroupJoin(ticketsByMonth,
                    all => new { all.Year, all.Month },
                    ticket => new { ticket.Year, ticket.Month },
                    (all, ticketGroup) => new
                    {
                        Year = all.Year,
                        Month = all.Month,
                        MonthName = all.MonthName,
                        TotalJobs = ticketGroup.Any() ? ticketGroup.Sum(t => t.TotalJobs) : 0,
                        CompletedJobs = ticketGroup.Any() ? ticketGroup.Sum(t => t.CompletedJobs) : 0,
                        TotalRevenue = ticketGroup.Any() ? ticketGroup.Sum(t => t.TotalRevenue) : 0
                    })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            return result;
        }

        /// <summary>
        /// Sends an email notification about insurance status update
        /// </summary>
        private async Task NotifyInsuranceStatusUpdate(Vendor vendor)
        {
            // Get vendor's primary email
            var vendorEmail = vendor.EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
            if (string.IsNullOrEmpty(vendorEmail))
                return;

            // Send the notification
            await _emailService.SendVendorInsuranceUpdateAsync(vendor);
        }

        #endregion Helper Methods
    }
}