using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using Roovia.Services.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roovia.Services
{
    public class PropertyOwnerService : IPropertyOwner
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<PropertyOwnerService> _logger;
        private readonly IEmailService _emailService;

        public PropertyOwnerService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<PropertyOwnerService> logger,
            IEmailService emailService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Validate bank account if provided
                if (propertyOwner.BankAccount != null)
                {
                    var bankValidator = new BankAccountValidator();
                    var bankValidationResult = bankValidator.Validate(propertyOwner.BankAccount);
                    if (!bankValidationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", bankValidationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }
                }

                // Validate address
                if (propertyOwner.Address != null)
                {
                    var addressValidator = new AddressValidator();
                    var addressValidationResult = addressValidator.Validate(propertyOwner.Address);
                    if (!addressValidationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", addressValidationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }
                }

                // Set audit fields
                propertyOwner.CreatedOn = DateTime.Now;

                // Add the property owner
                await context.PropertyOwners.AddAsync(propertyOwner);
                await context.SaveChangesAsync();

                // Add primary email if provided
                if (!string.IsNullOrEmpty(propertyOwner.CreatedBy) && propertyOwner.EmailAddresses.Any())
                {
                    foreach (var email in propertyOwner.EmailAddresses)
                    {
                        email.SetRelatedEntity("PropertyOwner", propertyOwner.Id);
                        email.CreatedOn = DateTime.Now;
                        email.CreatedBy = propertyOwner.CreatedBy;
                    }
                }

                // Add primary contact if provided
                if (!string.IsNullOrEmpty(propertyOwner.CreatedBy) && propertyOwner.ContactNumbers.Any())
                {
                    foreach (var contact in propertyOwner.ContactNumbers)
                    {
                        contact.SetRelatedEntity("PropertyOwner", propertyOwner.Id);
                        contact.CreatedOn = DateTime.Now;
                        contact.CreatedBy = propertyOwner.CreatedBy;
                    }
                }

                await context.SaveChangesAsync();

                // Reload with related data
                var createdOwner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses)
                    .Include(o => o.ContactNumbers)
                    .Include(o => o.Properties)
                    .Include(o => o.Company)
                    .FirstOrDefaultAsync(o => o.Id == propertyOwner.Id);

                response.Response = createdOwner;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owner created successfully.";

                _logger.LogInformation("Property owner created with ID: {OwnerId} by {CreatedBy}", propertyOwner.Id, propertyOwner.CreatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property owner");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyOwnerById(int companyId, int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Status)
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.MainImage)
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Tenants.Where(t => !t.IsRemoved && t.StatusId == 1)) // Active tenants
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Beneficiaries)
                    .Include(o => o.Company)
                    .Where(o => o.Id == id && o.CompanyId == companyId && !o.IsRemoved)
                    .FirstOrDefaultAsync();

                if (owner != null)
                {
                    response.Response = owner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owner retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property owner {OwnerId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedOwner)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses)
                    .Include(o => o.ContactNumbers)
                    .FirstOrDefaultAsync(o => o.Id == id && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Validate bank account if provided
                if (updatedOwner.BankAccount != null)
                {
                    var bankValidator = new BankAccountValidator();
                    var bankValidationResult = bankValidator.Validate(updatedOwner.BankAccount);
                    if (!bankValidationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", bankValidationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }
                }

                // Validate address
                if (updatedOwner.Address != null)
                {
                    var addressValidator = new AddressValidator();
                    var addressValidationResult = addressValidator.Validate(updatedOwner.Address);
                    if (!addressValidationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", addressValidationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }
                }

                // Update owner properties
                owner.FirstName = updatedOwner.FirstName;
                owner.LastName = updatedOwner.LastName;
                owner.IdNumber = updatedOwner.IdNumber;
                owner.VatNumber = updatedOwner.VatNumber;
                owner.Address = updatedOwner.Address;
                owner.BankAccount = updatedOwner.BankAccount;
                owner.IsEmailNotificationsEnabled = updatedOwner.IsEmailNotificationsEnabled;
                owner.IsSmsNotificationsEnabled = updatedOwner.IsSmsNotificationsEnabled;
                owner.CustomerRef = updatedOwner.CustomerRef;
                owner.Tags = updatedOwner.Tags;
                owner.UpdatedDate = DateTime.Now;
                owner.UpdatedBy = updatedOwner.UpdatedBy;

                await context.SaveChangesAsync();

                // Reload with related data
                var updatedResult = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Status)
                    .Include(o => o.Company)
                    .FirstOrDefaultAsync(o => o.Id == id);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owner updated successfully.";

                _logger.LogInformation("Property owner updated: {OwnerId} by {UpdatedBy}", id, updatedOwner.UpdatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property owner {OwnerId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeletePropertyOwner(int id, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.Properties)
                    .FirstOrDefaultAsync(o => o.Id == id && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Check if owner has active properties
                if (owner.Properties.Any(p => !p.IsRemoved))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete property owner with active properties.";
                    return response;
                }

                // Soft delete
                owner.IsRemoved = true;
                owner.RemovedDate = DateTime.Now;
                owner.RemovedBy = user?.Id;

                // Deactivate associated emails and contacts
                foreach (var email in owner.EmailAddresses)
                {
                    email.IsActive = false;
                    email.UpdatedDate = DateTime.Now;
                    email.UpdatedBy = user?.Id;
                }

                foreach (var contact in owner.ContactNumbers)
                {
                    contact.IsActive = false;
                    contact.UpdatedDate = DateTime.Now;
                    contact.UpdatedBy = user?.Id;
                }

                await context.SaveChangesAsync();

                response.Response = owner;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owner deleted successfully.";

                _logger.LogInformation("Property owner soft deleted: {OwnerId} by {UserId}", id, user?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property owner {OwnerId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllPropertyOwners(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owners = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Status)
                    .Include(o => o.Company)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName)
                    .ToListAsync();

                response.Response = owners;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owners retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} property owners for company {CompanyId}", owners.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property owners for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving property owners: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddEmailAddress(int ownerId, Email email)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses)
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Validate email
                var emailValidator = new EmailValidator();
                var validationResult = emailValidator.Validate(email);
                if (!validationResult.IsValid)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return response;
                }

                // If marking as primary, unset other primary emails
                if (email.IsPrimary)
                {
                    foreach (var existingEmail in owner.EmailAddresses.Where(e => e.IsPrimary))
                    {
                        existingEmail.IsPrimary = false;
                        existingEmail.UpdatedDate = DateTime.Now;
                    }
                }

                // Set relation properties
                email.SetRelatedEntity("PropertyOwner", ownerId);
                email.CreatedOn = DateTime.Now;
                email.CreatedBy = owner.UpdatedBy;

                context.Emails.Add(email);
                await context.SaveChangesAsync();

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address added successfully.";

                _logger.LogInformation("Email address added for property owner {OwnerId}: {Email}", ownerId, email.EmailAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email address for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddContactNumber(int ownerId, ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.ContactNumbers)
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Validate contact number
                var contactValidator = new ContactNumberValidator();
                var validationResult = contactValidator.Validate(contactNumber);
                if (!validationResult.IsValid)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return response;
                }

                // If marking as primary, unset other primary numbers
                if (contactNumber.IsPrimary)
                {
                    foreach (var existingNumber in owner.ContactNumbers.Where(c => c.IsPrimary))
                    {
                        existingNumber.IsPrimary = false;
                        existingNumber.UpdatedDate = DateTime.Now;
                    }
                }

                // Set relation properties
                contactNumber.SetRelatedEntity("PropertyOwner", ownerId);
                contactNumber.CreatedOn = DateTime.Now;
                contactNumber.CreatedBy = owner.UpdatedBy;

                context.ContactNumbers.Add(contactNumber);
                await context.SaveChangesAsync();

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number added successfully.";

                _logger.LogInformation("Contact number added for property owner {OwnerId}: {Number}", ownerId, contactNumber.Number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact number for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the contact number: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyOwnersByPage(int companyId, int pageNumber, int pageSize)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Status)
                    .Include(o => o.Company)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName);

                var totalCount = await query.CountAsync();
                var owners = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                response.Response = new
                {
                    Data = owners,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owners retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated property owners for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving property owners: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateOwnerAddress(int ownerId, Address newAddress, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Validate address
                var addressValidator = new AddressValidator();
                var validationResult = addressValidator.Validate(newAddress);
                if (!validationResult.IsValid)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return response;
                }

                owner.Address = newAddress;
                owner.UpdatedDate = DateTime.Now;
                owner.UpdatedBy = userId;

                await context.SaveChangesAsync();

                response.Response = owner;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Address updated successfully.";

                _logger.LogInformation("Address updated for property owner {OwnerId} by {UserId}", ownerId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateOwnerBankAccount(int ownerId, BankAccount bankAccount, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Validate bank account
                var bankValidator = new BankAccountValidator();
                var validationResult = bankValidator.Validate(bankAccount);
                if (!validationResult.IsValid)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return response;
                }

                owner.BankAccount = bankAccount;
                owner.UpdatedDate = DateTime.Now;
                owner.UpdatedBy = userId;

                await context.SaveChangesAsync();

                response.Response = owner;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Bank account updated successfully.";

                _logger.LogInformation("Bank account updated for property owner {OwnerId} by {UserId}", ownerId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank account for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the bank account: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetOwnerByVatNumber(string vatNumber, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                    .Include(o => o.Company)
                    .Where(o => o.VatNumber == vatNumber && o.CompanyId == companyId && !o.IsRemoved)
                    .FirstOrDefaultAsync();

                if (owner != null)
                {
                    response.Response = owner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owner found.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found with the specified VAT number.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property owner by VAT number {VatNumber}", vatNumber);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetOwnerByIdNumber(string idNumber, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                    .Include(o => o.Company)
                    .Where(o => o.IdNumber == idNumber && o.CompanyId == companyId && !o.IsRemoved)
                    .FirstOrDefaultAsync();

                if (owner != null)
                {
                    response.Response = owner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owner found.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found with the specified ID number.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property owner by ID number {IdNumber}", idNumber);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> SearchOwners(int companyId, string searchTerm)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                    .Include(o => o.Company)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(o =>
                        o.FirstName.ToLower().Contains(searchTerm) ||
                        o.LastName.ToLower().Contains(searchTerm) ||
                        o.IdNumber.ToLower().Contains(searchTerm) ||
                        o.VatNumber.ToLower().Contains(searchTerm) ||
                        o.CustomerRef.ToLower().Contains(searchTerm) ||
                        o.EmailAddresses.Any(e => e.EmailAddress.ToLower().Contains(searchTerm)) ||
                        o.ContactNumbers.Any(c => c.Number.Contains(searchTerm))
                    );
                }

                var owners = await query
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName)
                    .Take(50) // Limit results for performance
                    .ToListAsync();

                response.Response = owners;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Found {owners.Count} owners matching '{searchTerm}'";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching property owners for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while searching property owners: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetOwnerStatistics(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owners = await context.PropertyOwners
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Tenants)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .ToListAsync();

                var statistics = new
                {
                    TotalOwners = owners.Count,
                    OwnersWithProperties = owners.Count(o => o.Properties.Any(p => !p.IsRemoved)),
                    OwnersWithoutProperties = owners.Count(o => !o.Properties.Any(p => !p.IsRemoved)),
                    TotalProperties = owners.SelectMany(o => o.Properties).Count(p => !p.IsRemoved),
                    PropertiesWithTenants = owners.SelectMany(o => o.Properties)
                        .Count(p => !p.IsRemoved && p.Tenants.Any(t => !t.IsRemoved && t.StatusId == 1)),
                    VacantProperties = owners.SelectMany(o => o.Properties)
                        .Count(p => !p.IsRemoved && !p.Tenants.Any(t => !t.IsRemoved && t.StatusId == 1)),
                    TotalMonthlyRevenue = owners.SelectMany(o => o.Properties)
                        .Where(p => !p.IsRemoved && p.HasTenant)
                        .Sum(p => p.RentalAmount),
                    OwnersByProvince = owners
                        .Where(o => o.Address != null && !string.IsNullOrEmpty(o.Address.Province))
                        .GroupBy(o => o.Address.Province)
                        .Select(g => new { Province = g.Key, Count = g.Count() })
                        .ToList(),
                    OwnersByCity = owners
                        .Where(o => o.Address != null && !string.IsNullOrEmpty(o.Address.City))
                        .GroupBy(o => o.Address.City)
                        .Select(g => new { City = g.Key, Count = g.Count() })
                        .ToList()
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Owner statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving owner statistics for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving statistics: " + ex.Message;
            }

            return response;
        }

        // Export functionality
        public async Task<ResponseModel> ExportOwnersToExcel(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owners = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                    .Include(o => o.Company)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName)
                    .ToListAsync();

                // Create Excel export (this would use a library like EPPlus or ClosedXML)
                var exportData = owners.Select(o => new
                {
                    FirstName = o.FirstName,
                    LastName = o.LastName,
                    IdNumber = o.IdNumber,
                    VatNumber = o.VatNumber,
                    Email = o.PrimaryEmail,
                    Phone = o.PrimaryContactNumber,
                    Address = o.Address != null ? $"{o.Address.Street}, {o.Address.City}, {o.Address.Province}" : "",
                    PropertyCount = o.Properties.Count(p => !p.IsRemoved),
                    MonthlyRevenue = o.Properties.Where(p => !p.IsRemoved && p.HasTenant).Sum(p => p.RentalAmount),
                    BankName = o.BankAccount?.BankName?.ToString(),
                    AccountNumber = o.BankAccount?.AccountNumber,
                    CustomerReference = o.CustomerRef,
                    Tags = o.Tags
                }).ToList();

                // TODO: Implement actual Excel export logic
                response.Response = exportData;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owners exported successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting property owners for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while exporting property owners: " + ex.Message;
            }

            return response;
        }
    }
}