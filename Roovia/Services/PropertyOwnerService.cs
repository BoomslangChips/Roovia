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
using System.Transactions;

namespace Roovia.Services
{
    /// <summary>
    /// Service for managing property owners
    /// </summary>
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
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        /// <summary>
        /// Creates a new property owner
        /// </summary>
        public async Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner)
        {
            ResponseModel response = new();

            try
            {
                // Validate required fields
                if (propertyOwner.PropertyOwnerTypeId == 1) // Individual
                {
                    if (string.IsNullOrEmpty(propertyOwner.FirstName) || string.IsNullOrEmpty(propertyOwner.LastName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "First name and last name are required for individual owners.";
                        return response;
                    }
                }
                else // Company/Organization
                {
                    if (string.IsNullOrEmpty(propertyOwner.CompanyName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Company name is required for organization owners.";
                        return response;
                    }
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
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
                    await transaction.CommitAsync();

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
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property owner");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property owner: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets a property owner by ID
        /// </summary>
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
                    .AsNoTracking() // Improves read-only query performance
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

        /// <summary>
        /// Updates an existing property owner
        /// </summary>
        public async Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedOwner)
        {
            ResponseModel response = new();

            try
            {
                // Validate required fields
                if (updatedOwner.PropertyOwnerTypeId == 1) // Individual
                {
                    if (string.IsNullOrEmpty(updatedOwner.FirstName) || string.IsNullOrEmpty(updatedOwner.LastName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "First name and last name are required for individual owners.";
                        return response;
                    }
                }
                else // Company/Organization
                {
                    if (string.IsNullOrEmpty(updatedOwner.CompanyName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Company name is required for organization owners.";
                        return response;
                    }
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
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
                    owner.CompanyName = updatedOwner.CompanyName;
                    owner.PropertyOwnerTypeId = updatedOwner.PropertyOwnerTypeId;
                    owner.IdNumber = updatedOwner.IdNumber;
                    owner.RegistrationNumber = updatedOwner.RegistrationNumber;
                    owner.VatNumber = updatedOwner.VatNumber;
                    owner.ContactPerson = updatedOwner.ContactPerson;
                    owner.Address = updatedOwner.Address;
                    owner.BankAccount = updatedOwner.BankAccount;
                    owner.CustomerRef = updatedOwner.CustomerRef;
                    owner.Tags = updatedOwner.Tags;
                    owner.UpdatedDate = DateTime.Now;
                    owner.UpdatedBy = updatedOwner.UpdatedBy;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

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
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property owner {OwnerId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the property owner: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Deletes a property owner (soft delete)
        /// </summary>
        public async Task<ResponseModel> DeletePropertyOwner(int id, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var owner = await context.PropertyOwners
                        .Include(o => o.Properties)
                        .Include(o => o.EmailAddresses)
                        .Include(o => o.ContactNumbers)
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
                    await transaction.CommitAsync();

                    response.Response = owner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owner deleted successfully.";

                    _logger.LogInformation("Property owner soft deleted: {OwnerId} by {UserId}", id, user?.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property owner {OwnerId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the property owner: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets all property owners for a company
        /// </summary>
        public async Task<ResponseModel> GetAllPropertyOwners(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owners = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties.Where(p => !p.IsRemoved))
                        .ThenInclude(p => p.Status)
                    .Include(o => o.Company)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName)
                    .AsNoTracking() // Improves read-only query performance
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

        /// <summary>
        /// Adds an email address to a property owner
        /// </summary>
        public async Task<ResponseModel> AddEmailAddress(int ownerId, Email email)
        {
            ResponseModel response = new();

            try
            {
                if (string.IsNullOrEmpty(email.EmailAddress))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Email address is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
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

                    await context.Emails.AddAsync(email);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Response = email;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Email address added successfully.";

                    _logger.LogInformation("Email address added for property owner {OwnerId}: {Email}", ownerId, email.EmailAddress);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email address for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the email address: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Adds a contact number to a property owner
        /// </summary>
        public async Task<ResponseModel> AddContactNumber(int ownerId, ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                if (string.IsNullOrEmpty(contactNumber.Number))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Contact number is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
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

                    await context.ContactNumbers.AddAsync(contactNumber);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Response = contactNumber;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Contact number added successfully.";

                    _logger.LogInformation("Contact number added for property owner {OwnerId}: {Number}", ownerId, contactNumber.Number);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact number for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the contact number: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets property owners with pagination
        /// </summary>
        public async Task<ResponseModel> GetPropertyOwnersByPage(int companyId, int pageNumber, int pageSize)
        {
            ResponseModel response = new();

            try
            {
                if (pageNumber < 1)
                {
                    pageNumber = 1;
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    pageSize = 20; // Default page size with reasonable limit
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                // First get total count for pagination info
                var totalCount = await context.PropertyOwners
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .CountAsync();

                // Then get the actual data for the page
                var query = context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties.Where(p => !p.IsRemoved))
                        .ThenInclude(p => p.Status)
                    .Include(o => o.Company)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName)
                    .AsNoTracking(); // Improves read-only query performance

                var owners = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                response.Response = new
                {
                    Data = owners,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < totalPages
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

        /// <summary>
        /// Updates the address for a property owner
        /// </summary>
        public async Task<ResponseModel> UpdateOwnerAddress(int ownerId, Address newAddress, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
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
                    await transaction.CommitAsync();

                    response.Response = owner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Address updated successfully.";

                    _logger.LogInformation("Address updated for property owner {OwnerId} by {UserId}", ownerId, userId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the address: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Updates the bank account for a property owner
        /// </summary>
        public async Task<ResponseModel> UpdateOwnerBankAccount(int ownerId, BankAccount bankAccount, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
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
                    await transaction.CommitAsync();

                    response.Response = owner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Bank account updated successfully.";

                    _logger.LogInformation("Bank account updated for property owner {OwnerId} by {UserId}", ownerId, userId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank account for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the bank account: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        ///
        /// <summary>
        /// Gets a property owner by VAT number
        /// </summary>
        public async Task<ResponseModel> GetOwnerByVatNumber(string vatNumber, int companyId)
        {
            ResponseModel response = new();

            try
            {
                if (string.IsNullOrEmpty(vatNumber))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "VAT number is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties.Where(p => !p.IsRemoved))
                    .Include(o => o.Company)
                    .Where(o => o.VatNumber == vatNumber && o.CompanyId == companyId && !o.IsRemoved)
                    .AsNoTracking()
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

        /// <summary>
        /// Gets a property owner by ID number
        /// </summary>
        public async Task<ResponseModel> GetOwnerByIdNumber(string idNumber, int companyId)
        {
            ResponseModel response = new();

            try
            {
                if (string.IsNullOrEmpty(idNumber))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "ID number is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties.Where(p => !p.IsRemoved))
                    .Include(o => o.Company)
                    .Where(o => o.IdNumber == idNumber && o.CompanyId == companyId && !o.IsRemoved)
                    .AsNoTracking()
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

        /// <summary>
        /// Searches for property owners based on a search term
        /// </summary>
        public async Task<ResponseModel> SearchOwners(int companyId, string searchTerm)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                IQueryable<PropertyOwner> query = context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties.Where(p => !p.IsRemoved))
                    .Include(o => o.Company)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower().Trim();
                    query = query.Where(o =>
                        (o.FirstName != null && o.FirstName.ToLower().Contains(searchTerm)) ||
                        (o.LastName != null && o.LastName.ToLower().Contains(searchTerm)) ||
                        (o.CompanyName != null && o.CompanyName.ToLower().Contains(searchTerm)) ||
                        (o.IdNumber != null && o.IdNumber.ToLower().Contains(searchTerm)) ||
                        (o.VatNumber != null && o.VatNumber.ToLower().Contains(searchTerm)) ||
                        (o.CustomerRef != null && o.CustomerRef.ToLower().Contains(searchTerm)) ||
                        o.EmailAddresses.Any(e => e.IsActive && e.EmailAddress.ToLower().Contains(searchTerm)) ||
                        o.ContactNumbers.Any(c => c.IsActive && c.Number.Contains(searchTerm))
                    );
                }

                // Apply a reasonable limit to avoid performance issues
                var owners = await query
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName)
                    .Take(50)
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

        /// <summary>
        /// Gets statistics about property owners for a company
        /// </summary>
        public async Task<ResponseModel> GetOwnerStatistics(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Use optimized query approach to reduce memory usage
                var owners = await context.PropertyOwners
                    .Include(o => o.Properties.Where(p => !p.IsRemoved))
                        .ThenInclude(p => p.Tenants.Where(t => !t.IsRemoved && t.StatusId == 1))
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .AsNoTracking()
                    .ToListAsync();

                var statistics = new
                {
                    TotalOwners = owners.Count,
                    OwnersWithProperties = owners.Count(o => o.Properties.Any()),
                    OwnersWithoutProperties = owners.Count(o => !o.Properties.Any()),
                    TotalProperties = owners.Sum(o => o.Properties.Count),
                    PropertiesWithTenants = owners.Sum(o => o.Properties.Count(p => p.Tenants.Any())),
                    VacantProperties = owners.Sum(o => o.Properties.Count(p => !p.Tenants.Any())),
                    TotalMonthlyRevenue = owners.Sum(o =>
                        o.Properties.Where(p => p.HasTenant)
                        .Sum(p => p.RentalAmount)),
                    OwnersByType = owners.GroupBy(o => o.PropertyOwnerTypeId)
                        .Select(g => new { TypeId = g.Key, Count = g.Count() })
                        .ToList(),
                    OwnersByProvince = owners
                        .Where(o => o.Address != null && !string.IsNullOrEmpty(o.Address.Province))
                        .GroupBy(o => o.Address.Province)
                        .Select(g => new { Province = g.Key, Count = g.Count() })
                        .ToList(),
                    OwnersByCity = owners
                        .Where(o => o.Address != null && !string.IsNullOrEmpty(o.Address.City))
                        .GroupBy(o => o.Address.City)
                        .Select(g => new { City = g.Key, Count = g.Count() })
                        .ToList(),
                    AveragePropertiesPerOwner = owners.Count > 0 ?
                        Math.Round((double)owners.Sum(o => o.Properties.Count) / owners.Count, 2) : 0
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

        /// <summary>
        /// Exports property owners data to Excel (currently returns data for export)
        /// </summary>
        public async Task<ResponseModel> ExportOwnersToExcel(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owners = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties.Where(p => !p.IsRemoved))
                        .ThenInclude(p => p.Status)
                    .Include(o => o.BankAccount)
                        .ThenInclude(ba => ba.BankName)
                    .Include(o => o.Company)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName)
                    .AsNoTracking()
                    .ToListAsync();

                // Create export data structure
                var exportData = owners.Select(o => new
                {
                    Type = o.PropertyOwnerTypeId == 1 ? "Individual" : "Company/Organization",
                    Name = o.PropertyOwnerTypeId == 1 ? $"{o.FirstName} {o.LastName}" : o.CompanyName,
                    o.FirstName,
                    o.LastName,
                    o.CompanyName,
                    o.IdNumber,
                    o.RegistrationNumber,
                    o.VatNumber,
                    EmailAddress = o.PrimaryEmail,
                    ContactNumber = o.PrimaryContactNumber,
                    Address = o.Address != null ?
                        $"{o.Address.Street}, {o.Address.City}, {o.Address.Province}, {o.Address.PostalCode}" : "",
                    City = o.Address?.City,
                    Province = o.Address?.Province,
                    Country = o.Address?.Country,
                    BankName = o.BankAccount?.BankName?.Name,
                    AccountNumber = o.BankAccount?.AccountNumber,
                    BranchCode = o.BankAccount?.BranchCode,
                    CustomerReference = o.CustomerRef,
                    PropertiesCount = o.Properties.Count,
                    PropertiesWithTenants = o.Properties.Count(p => p.HasTenant),
                    MonthlyRevenue = o.Properties.Where(p => p.HasTenant).Sum(p => p.RentalAmount),
                    CreatedDate = o.CreatedOn,
                    o.Tags
                }).ToList();

                // TODO: In a real implementation, this would be converted to an Excel file
                // using a library like EPPlus, ClosedXML, or NPOI

                response.Response = exportData;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owners data prepared for export successfully.";
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