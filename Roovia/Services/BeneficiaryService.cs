using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using System.Transactions;

namespace Roovia.Services
{
    public class BeneficiaryService : IBeneficiary
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<BeneficiaryService> _logger;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKeyPrefix = "Beneficiary_";

        public BeneficiaryService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<BeneficiaryService> logger,
            IEmailService emailService,
            IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<ResponseModel> CreateBeneficiary(PropertyBeneficiary beneficiary, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the property exists and belongs to the company
                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == beneficiary.PropertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found or does not belong to the company.";
                    return response;
                }

                // Validate bank account if provided
                if (beneficiary.BankAccount != null)
                {
                    var bankValidator = new BankAccountValidator();
                    var bankValidationResult = bankValidator.Validate(beneficiary.BankAccount);
                    if (!bankValidationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", bankValidationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }
                }

                // Validate address
                if (beneficiary.Address != null)
                {
                    var addressValidator = new AddressValidator();
                    var addressValidationResult = addressValidator.Validate(beneficiary.Address);
                    if (!addressValidationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", addressValidationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }
                }

                // Set audit fields
                beneficiary.CompanyId = companyId;
                beneficiary.CreatedOn = DateTime.Now;

                // Calculate beneficiary amount based on commission type
                if (beneficiary.CommissionTypeId == 1) // Percentage
                {
                    beneficiary.Amount = property.RentalAmount * (beneficiary.CommissionValue / 100);
                }
                else if (beneficiary.CommissionTypeId == 2) // Fixed amount
                {
                    beneficiary.Amount = beneficiary.CommissionValue;
                }

                beneficiary.PropertyAmount = property.RentalAmount;

                // Add the beneficiary
                await context.PropertyBeneficiaries.AddAsync(beneficiary);
                await context.SaveChangesAsync();

                // Add primary email if provided
                if (beneficiary.EmailAddresses != null && beneficiary.EmailAddresses.Any())
                {
                    foreach (var email in beneficiary.EmailAddresses)
                    {
                        email.SetRelatedEntity("PropertyBeneficiary", beneficiary.Id);
                        email.CreatedOn = DateTime.Now;
                        email.CreatedBy = beneficiary.CreatedBy;
                    }
                }

                // Add primary contact if provided
                if (beneficiary.ContactNumbers != null && beneficiary.ContactNumbers.Any())
                {
                    foreach (var contact in beneficiary.ContactNumbers)
                    {
                        contact.SetRelatedEntity("PropertyBeneficiary", beneficiary.Id);
                        contact.CreatedOn = DateTime.Now;
                        contact.CreatedBy = beneficiary.CreatedBy;
                    }
                }

                await context.SaveChangesAsync();

                // Reload with related data
                var createdBeneficiary = await GetBeneficiaryWithDetails(context, beneficiary.Id);

                scope.Complete();

                // Invalidate any cached beneficiary lists
                InvalidateCachedBeneficiaryLists(companyId, beneficiary.PropertyId);

                response.Response = createdBeneficiary;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary created successfully.";

                _logger.LogInformation("Beneficiary created with ID: {BeneficiaryId} for property {PropertyId} by {CreatedBy}",
                    beneficiary.Id, beneficiary.PropertyId, beneficiary.CreatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating beneficiary");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the beneficiary: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBeneficiaryById(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                // Try to get from cache first
                var cacheKey = $"{_cacheKeyPrefix}{id}";
                if (_cache.TryGetValue(cacheKey, out PropertyBeneficiary cachedBeneficiary))
                {
                    response.Response = cachedBeneficiary;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Beneficiary retrieved from cache successfully.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Property)
                        .ThenInclude(p => p.Owner)
                    .Include(b => b.Property)
                        .ThenInclude(p => p.Address)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Include(b => b.Payments)
                        .ThenInclude(p => p.Status)
                    .Include(b => b.Company)
                    .Where(b => b.Id == id && b.CompanyId == companyId && b.IsActive)
                    .FirstOrDefaultAsync();

                if (beneficiary != null)
                {
                    // Cache the result
                    _cache.Set(cacheKey, beneficiary, TimeSpan.FromMinutes(15));

                    response.Response = beneficiary;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Beneficiary retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving beneficiary {BeneficiaryId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the beneficiary: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateBeneficiary(int id, PropertyBeneficiary updatedBeneficiary, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses)
                    .Include(b => b.ContactNumbers)
                    .Include(b => b.Property)
                    .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                    return response;
                }

                // Validate bank account if provided
                if (updatedBeneficiary.BankAccount != null)
                {
                    var bankValidator = new BankAccountValidator();
                    var bankValidationResult = bankValidator.Validate(updatedBeneficiary.BankAccount);
                    if (!bankValidationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", bankValidationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }
                }

                // Validate address
                if (updatedBeneficiary.Address != null)
                {
                    var addressValidator = new AddressValidator();
                    var addressValidationResult = addressValidator.Validate(updatedBeneficiary.Address);
                    if (!addressValidationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", addressValidationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }
                }

                // Update beneficiary fields
                beneficiary.Name = updatedBeneficiary.Name;
                beneficiary.Address = updatedBeneficiary.Address;
                beneficiary.BenTypeId = updatedBeneficiary.BenTypeId;
                beneficiary.CommissionTypeId = updatedBeneficiary.CommissionTypeId;
                beneficiary.CommissionValue = updatedBeneficiary.CommissionValue;

                // Recalculate amount if commission changed
                if (beneficiary.CommissionTypeId == 1) // Percentage
                {
                    beneficiary.Amount = beneficiary.Property.RentalAmount * (beneficiary.CommissionValue / 100);
                }
                else if (beneficiary.CommissionTypeId == 2) // Fixed amount
                {
                    beneficiary.Amount = beneficiary.CommissionValue;
                }

                beneficiary.PropertyAmount = updatedBeneficiary.PropertyAmount;
                beneficiary.BenStatusId = updatedBeneficiary.BenStatusId;
                beneficiary.CustomerRefBeneficiary = updatedBeneficiary.CustomerRefBeneficiary;
                beneficiary.CustomerRefProperty = updatedBeneficiary.CustomerRefProperty;
                beneficiary.Agent = updatedBeneficiary.Agent;
                beneficiary.Tags = updatedBeneficiary.Tags;
                beneficiary.BankAccount = updatedBeneficiary.BankAccount;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = updatedBeneficiary.UpdatedBy;

                // Update emails if provided
                if (updatedBeneficiary.EmailAddresses != null && updatedBeneficiary.EmailAddresses.Any())
                {
                    // Handle primary email change
                    var newPrimaryEmail = updatedBeneficiary.EmailAddresses.FirstOrDefault(e => e.IsPrimary);
                    if (newPrimaryEmail != null)
                    {
                        var existingPrimary = beneficiary.EmailAddresses.FirstOrDefault(e => e.IsPrimary);
                        if (existingPrimary != null)
                        {
                            existingPrimary.IsPrimary = false;
                            existingPrimary.UpdatedDate = DateTime.Now;
                            existingPrimary.UpdatedBy = updatedBeneficiary.UpdatedBy;
                        }

                        if (newPrimaryEmail.Id == 0)
                        {
                            newPrimaryEmail.SetRelatedEntity("PropertyBeneficiary", beneficiary.Id);
                            newPrimaryEmail.CreatedOn = DateTime.Now;
                            newPrimaryEmail.CreatedBy = updatedBeneficiary.UpdatedBy;
                            beneficiary.EmailAddresses.Add(newPrimaryEmail);
                        }
                        else
                        {
                            var existingEmail = beneficiary.EmailAddresses.FirstOrDefault(e => e.Id == newPrimaryEmail.Id);
                            if (existingEmail != null)
                            {
                                existingEmail.IsPrimary = true;
                                existingEmail.UpdatedDate = DateTime.Now;
                                existingEmail.UpdatedBy = updatedBeneficiary.UpdatedBy;
                            }
                        }
                    }
                }

                // Update contact numbers if provided
                if (updatedBeneficiary.ContactNumbers != null && updatedBeneficiary.ContactNumbers.Any())
                {
                    // Handle primary contact change
                    var newPrimaryContact = updatedBeneficiary.ContactNumbers.FirstOrDefault(c => c.IsPrimary);
                    if (newPrimaryContact != null)
                    {
                        var existingPrimary = beneficiary.ContactNumbers.FirstOrDefault(c => c.IsPrimary);
                        if (existingPrimary != null)
                        {
                            existingPrimary.IsPrimary = false;
                            existingPrimary.UpdatedDate = DateTime.Now;
                            existingPrimary.UpdatedBy = updatedBeneficiary.UpdatedBy;
                        }

                        if (newPrimaryContact.Id == 0)
                        {
                            newPrimaryContact.SetRelatedEntity("PropertyBeneficiary", beneficiary.Id);
                            newPrimaryContact.CreatedOn = DateTime.Now;
                            newPrimaryContact.CreatedBy = updatedBeneficiary.UpdatedBy;
                            beneficiary.ContactNumbers.Add(newPrimaryContact);
                        }
                        else
                        {
                            var existingContact = beneficiary.ContactNumbers.FirstOrDefault(c => c.Id == newPrimaryContact.Id);
                            if (existingContact != null)
                            {
                                existingContact.IsPrimary = true;
                                existingContact.UpdatedDate = DateTime.Now;
                                existingContact.UpdatedBy = updatedBeneficiary.UpdatedBy;
                            }
                        }
                    }
                }

                await context.SaveChangesAsync();

                scope.Complete();

                // Invalidate caches
                var cacheKey = $"{_cacheKeyPrefix}{id}";
                _cache.Remove(cacheKey);
                InvalidateCachedBeneficiaryLists(companyId, beneficiary.PropertyId);

                // Reload with related data
                var updatedResult = await GetBeneficiaryWithDetails(context, id);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary updated successfully.";

                _logger.LogInformation("Beneficiary updated: {BeneficiaryId} by {UpdatedBy}", id, updatedBeneficiary.UpdatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating beneficiary {BeneficiaryId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the beneficiary: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteBeneficiary(int id, int companyId, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.Payments)
                    .Include(b => b.EmailAddresses)
                    .Include(b => b.ContactNumbers)
                    .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                    return response;
                }

                // Check if beneficiary has pending or processed payments
                if (beneficiary.Payments.Any(p => p.StatusId == 1 || p.StatusId == 2)) // Pending or Paid
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete beneficiary with pending or processed payments.";
                    return response;
                }

                // Soft delete
                beneficiary.IsActive = false;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = user?.Id;

                // Deactivate associated emails and contacts
                foreach (var email in beneficiary.EmailAddresses)
                {
                    email.IsActive = false;
                    email.UpdatedDate = DateTime.Now;
                    email.UpdatedBy = user?.Id;
                }

                foreach (var contact in beneficiary.ContactNumbers)
                {
                    contact.IsActive = false;
                    contact.UpdatedDate = DateTime.Now;
                    contact.UpdatedBy = user?.Id;
                }

                await context.SaveChangesAsync();

                scope.Complete();

                // Invalidate caches
                var cacheKey = $"{_cacheKeyPrefix}{id}";
                _cache.Remove(cacheKey);
                InvalidateCachedBeneficiaryLists(companyId, beneficiary.PropertyId);

                response.Response = beneficiary;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary deleted successfully.";

                _logger.LogInformation("Beneficiary soft deleted: {BeneficiaryId} by {UserId}", id, user?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting beneficiary {BeneficiaryId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the beneficiary: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBeneficiariesByProperty(int propertyId, int companyId, int page = 1, int pageSize = 20, string sortField = "Name", bool sortAscending = true)
        {
            ResponseModel response = new();

            try
            {
                // Try to get from cache first if not paged
                if (page == 1 && pageSize == int.MaxValue)
                {
                    var cacheKey = $"{_cacheKeyPrefix}ByProperty_{propertyId}";
                    if (_cache.TryGetValue(cacheKey, out List<PropertyBeneficiary> cachedBeneficiaries))
                    {
                        response.Response = new
                        {
                            Items = cachedBeneficiaries,
                            TotalCount = cachedBeneficiaries.Count,
                            Page = page,
                            PageSize = pageSize,
                            TotalPages = 1
                        };
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Beneficiaries retrieved from cache successfully.";
                        return response;
                    }
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                // Base query
                var query = context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Include(b => b.Payments)
                    .Where(b => b.PropertyId == propertyId && b.CompanyId == companyId && b.IsActive);

                // Apply sorting
                query = ApplySorting(query, sortField, sortAscending);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var beneficiaries = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Cache if not paged
                if (page == 1 && pageSize == int.MaxValue)
                {
                    var cacheKey = $"{_cacheKeyPrefix}ByProperty_{propertyId}";
                    _cache.Set(cacheKey, beneficiaries, TimeSpan.FromMinutes(15));
                }

                response.Response = new
                {
                    Items = beneficiaries,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiaries retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} beneficiaries for property {PropertyId}", beneficiaries.Count, propertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving beneficiaries for property {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving beneficiaries: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllBeneficiaries(int companyId, int page = 1, int pageSize = 20, string sortField = "Name", bool sortAscending = true, string filterField = null, string filterValue = null)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Base query
                var query = context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Property)
                        .ThenInclude(p => p.Owner)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Include(b => b.Payments)
                    .Include(b => b.Company)
                    .Where(b => b.CompanyId == companyId && b.IsActive);

                // Apply filtering
                if (!string.IsNullOrEmpty(filterField) && !string.IsNullOrEmpty(filterValue))
                {
                    query = ApplyFilter(query, filterField, filterValue);
                }

                // Apply sorting
                query = ApplySorting(query, sortField, sortAscending);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var beneficiaries = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                response.Response = new
                {
                    Items = beneficiaries,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiaries retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} beneficiaries for company {CompanyId}", beneficiaries.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving beneficiaries for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving beneficiaries: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddEmailAddress(int beneficiaryId, Email email)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses)
                    .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
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
                    foreach (var existingEmail in beneficiary.EmailAddresses.Where(e => e.IsPrimary))
                    {
                        existingEmail.IsPrimary = false;
                        existingEmail.UpdatedDate = DateTime.Now;
                    }
                }

                // Set relation properties
                email.SetRelatedEntity("PropertyBeneficiary", beneficiaryId);
                email.CreatedOn = DateTime.Now;
                email.CreatedBy = beneficiary.UpdatedBy;

                context.Emails.Add(email);
                await context.SaveChangesAsync();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{beneficiaryId}";
                _cache.Remove(cacheKey);

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address added successfully.";

                _logger.LogInformation("Email address added for beneficiary {BeneficiaryId}: {Email}", beneficiaryId, email.EmailAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email address for beneficiary {BeneficiaryId}", beneficiaryId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddContactNumber(int beneficiaryId, ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.ContactNumbers)
                    .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
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
                    foreach (var existingNumber in beneficiary.ContactNumbers.Where(c => c.IsPrimary))
                    {
                        existingNumber.IsPrimary = false;
                        existingNumber.UpdatedDate = DateTime.Now;
                    }
                }

                // Set relation properties
                contactNumber.SetRelatedEntity("PropertyBeneficiary", beneficiaryId);
                contactNumber.CreatedOn = DateTime.Now;
                contactNumber.CreatedBy = beneficiary.UpdatedBy;

                context.ContactNumbers.Add(contactNumber);
                await context.SaveChangesAsync();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{beneficiaryId}";
                _cache.Remove(cacheKey);

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number added successfully.";

                _logger.LogInformation("Contact number added for beneficiary {BeneficiaryId}: {Number}", beneficiaryId, contactNumber.Number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact number for beneficiary {BeneficiaryId}", beneficiaryId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the contact number: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateBeneficiaryStatus(int beneficiaryId, int statusId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                    return response;
                }

                var oldStatusId = beneficiary.BenStatusId;
                beneficiary.BenStatusId = statusId;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = userId;

                await context.SaveChangesAsync();

                // Load the status names for logging
                var newStatus = await context.BeneficiaryStatusTypes.FindAsync(statusId);
                var oldStatus = await context.BeneficiaryStatusTypes.FindAsync(oldStatusId);

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{beneficiaryId}";
                _cache.Remove(cacheKey);
                InvalidateCachedBeneficiaryLists(beneficiary.CompanyId, beneficiary.PropertyId);

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary status updated successfully.";

                _logger.LogInformation("Beneficiary status updated: {BeneficiaryId} from {OldStatus} to {NewStatus} by {UserId}",
                    beneficiaryId, oldStatus?.Name, newStatus?.Name, userId);

                // Send notification if status changed to inactive
                if (oldStatusId == 1 && statusId != 1)
                {
                    await SendStatusChangeNotification(beneficiary, oldStatus?.Name, newStatus?.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating beneficiary status {BeneficiaryId}", beneficiaryId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating beneficiary status: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateBankAccount(int beneficiaryId, BankAccount bankAccount, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
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

                beneficiary.BankAccount = bankAccount;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = userId;

                await context.SaveChangesAsync();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{beneficiaryId}";
                _cache.Remove(cacheKey);

                response.Response = beneficiary;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Bank account updated successfully.";

                _logger.LogInformation("Bank account updated for beneficiary: {BeneficiaryId} by {UserId}", beneficiaryId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank account for beneficiary {BeneficiaryId}", beneficiaryId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating bank account: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateBeneficiaryAddress(int beneficiaryId, Address newAddress, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
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

                beneficiary.Address = newAddress;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = userId;

                await context.SaveChangesAsync();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{beneficiaryId}";
                _cache.Remove(cacheKey);

                response.Response = beneficiary;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Address updated successfully.";

                _logger.LogInformation("Address updated for beneficiary {BeneficiaryId} by {UserId}", beneficiaryId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address for beneficiary {BeneficiaryId}", beneficiaryId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CalculateBeneficiaryAmounts(int propertyId, decimal paymentAmount)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiaries = await context.PropertyBeneficiaries
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenType)
                    .Include(b => b.BankAccount)
                        .ThenInclude(ba => ba.BankName)
                    .Where(b => b.PropertyId == propertyId && b.IsActive && b.BenStatusId == 1) // Active beneficiaries
                    .ToListAsync();

                var calculations = new List<dynamic>();
                decimal totalAllocated = 0;

                foreach (var beneficiary in beneficiaries)
                {
                    decimal amount = 0;

                    if (beneficiary.CommissionType?.Name == "Percentage")
                    {
                        amount = paymentAmount * (beneficiary.CommissionValue / 100);
                    }
                    else if (beneficiary.CommissionType?.Name == "Fixed")
                    {
                        amount = beneficiary.CommissionValue;
                    }

                    totalAllocated += amount;

                    calculations.Add(new
                    {
                        BeneficiaryId = beneficiary.Id,
                        BeneficiaryName = beneficiary.Name,
                        BeneficiaryType = beneficiary.BenType?.Name,
                        CommissionType = beneficiary.CommissionType?.Name,
                        beneficiary.CommissionValue,
                        CalculatedAmount = Math.Round(amount, 2),
                        BankAccount = beneficiary.BankAccount != null ?
                            $"{beneficiary.BankAccount.BankName?.Name} - {beneficiary.BankAccount.AccountNumber}" : "Not specified"
                    });
                }

                // Calculate remaining amount for property owner
                decimal remainingAmount = paymentAmount - totalAllocated;

                response.Response = new
                {
                    PaymentAmount = paymentAmount,
                    TotalAllocated = totalAllocated,
                    RemainingForOwner = remainingAmount,
                    BeneficiaryAllocations = calculations
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary amounts calculated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating beneficiary amounts for property {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while calculating amounts: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBeneficiariesByType(int companyId, int beneficiaryTypeId, int page = 1, int pageSize = 20)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Base query
                var query = context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Property)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Where(b => b.CompanyId == companyId && b.BenTypeId == beneficiaryTypeId && b.IsActive)
                    .OrderBy(b => b.Name);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var beneficiaries = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                response.Response = new
                {
                    Items = beneficiaries,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Retrieved {beneficiaries.Count} beneficiaries of the specified type.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving beneficiaries by type for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving beneficiaries: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBeneficiaryPaymentHistory(int beneficiaryId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.Payments)
                        .ThenInclude(p => p.PaymentAllocation)
                            .ThenInclude(pa => pa.Payment)
                    .Include(b => b.Payments)
                        .ThenInclude(p => p.Status)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.CompanyId == companyId);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                    return response;
                }

                var paymentHistory = beneficiary.Payments
                    .OrderByDescending(p => p.CreatedOn)
                    .Select(p => new
                    {
                        p.Id,
                        p.PaymentReference,
                        p.Amount,
                        Status = p.Status?.Name,
                        StatusId = p.StatusId,
                        p.PaymentDate,
                        p.TransactionReference,
                        PropertyPayment = p.PaymentAllocation?.Payment?.PaymentReference,
                        PropertyPaymentDate = p.PaymentAllocation?.Payment?.PaymentDate,
                        p.Notes,
                        p.CreatedOn
                    })
                    .ToList();

                var statistics = new
                {
                    TotalPayments = beneficiary.Payments.Count,
                    TotalAmount = beneficiary.Payments.Where(p => p.StatusId == 2).Sum(p => p.Amount), // Paid
                    PendingAmount = beneficiary.Payments.Where(p => p.StatusId == 1).Sum(p => p.Amount), // Pending
                    LastPaymentDate = beneficiary.Payments.Where(p => p.StatusId == 2)
                        .OrderByDescending(p => p.PaymentDate)
                        .FirstOrDefault()?.PaymentDate,
                    AveragePaymentAmount = beneficiary.Payments.Where(p => p.StatusId == 2).Any() ?
                        beneficiary.Payments.Where(p => p.StatusId == 2).Average(p => p.Amount) : 0,
                    MonthlyTrend = GetMonthlyPaymentTrend(beneficiary.Payments)
                };

                response.Response = new
                {
                    Beneficiary = new
                    {
                        beneficiary.Id,
                        beneficiary.Name,
                        Type = beneficiary.BenType?.Name,
                        Status = beneficiary.BenStatus?.Name,
                        CommissionType = beneficiary.CommissionType?.Name,
                        beneficiary.CommissionValue
                    },
                    Statistics = statistics,
                    PaymentHistory = paymentHistory
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Payment history retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment history for beneficiary {BeneficiaryId}", beneficiaryId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving payment history: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> SearchBeneficiaries(int companyId, string searchTerm, int page = 1, int pageSize = 20)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Property)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Where(b => b.CompanyId == companyId && b.IsActive);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(b =>
                        b.Name.ToLower().Contains(searchTerm) ||
                        b.CustomerRefBeneficiary.ToLower().Contains(searchTerm) ||
                        b.CustomerRefProperty.ToLower().Contains(searchTerm) ||
                        b.Agent.ToLower().Contains(searchTerm) ||
                        b.EmailAddresses.Any(e => e.EmailAddress.ToLower().Contains(searchTerm)) ||
                        b.ContactNumbers.Any(c => c.Number.Contains(searchTerm)) ||
                        b.Property.PropertyName.ToLower().Contains(searchTerm) ||
                        b.Property.PropertyCode.ToLower().Contains(searchTerm)
                    );
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var beneficiaries = await query
                    .OrderBy(b => b.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                response.Response = new
                {
                    Items = beneficiaries,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Found {totalCount} beneficiaries matching '{searchTerm}'";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching beneficiaries for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while searching beneficiaries: " + ex.Message;
            }

            return response;
        }

        #region Private Helper Methods

        private async Task<PropertyBeneficiary> GetBeneficiaryWithDetails(ApplicationDbContext context, int beneficiaryId)
        {
            return await context.PropertyBeneficiaries
                .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                .Include(b => b.Property)
                    .ThenInclude(p => p.Owner)
                .Include(b => b.BenType)
                .Include(b => b.CommissionType)
                .Include(b => b.BenStatus)
                .Include(b => b.Company)
                .FirstOrDefaultAsync(b => b.Id == beneficiaryId);
        }

        private async Task SendStatusChangeNotification(PropertyBeneficiary beneficiary, string oldStatus, string newStatus)
        {
            try
            {
                var primaryEmail = beneficiary.EmailAddresses?.FirstOrDefault(e => e.IsPrimary && e.IsActive)?.EmailAddress;
                if (string.IsNullOrEmpty(primaryEmail))
                    return;

                var emailContent = $@"
                    <p>Dear {beneficiary.Name},</p>
                    <p>Your beneficiary status has been updated.</p>
                    <p><strong>Previous Status:</strong> {oldStatus}</p>
                    <p><strong>New Status:</strong> {newStatus}</p>
                    <p>If you have any questions regarding this change, please contact our support team.</p>
                    <br>
                    <p>Best regards,<br>The Roovia Team</p>
                ";

                await _emailService.SendEmailAsync(
                    primaryEmail,
                    "Beneficiary Status Update",
                    emailContent
                );

                _logger.LogInformation("Status change notification sent to beneficiary {BeneficiaryId}", beneficiary.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending status change notification to beneficiary {BeneficiaryId}", beneficiary.Id);
                // Don't throw - notification failure shouldn't break the status update
            }
        }

        private IQueryable<PropertyBeneficiary> ApplySorting(IQueryable<PropertyBeneficiary> query, string sortField, bool sortAscending)
        {
            return sortField.ToLower() switch
            {
                "name" => sortAscending ? query.OrderBy(b => b.Name) : query.OrderByDescending(b => b.Name),
                "amount" => sortAscending ? query.OrderBy(b => b.Amount) : query.OrderByDescending(b => b.Amount),
                "type" => sortAscending ? query.OrderBy(b => b.BenType.Name) : query.OrderByDescending(b => b.BenType.Name),
                "status" => sortAscending ? query.OrderBy(b => b.BenStatus.Name) : query.OrderByDescending(b => b.BenStatus.Name),
                "property" => sortAscending ? query.OrderBy(b => b.Property.PropertyName) : query.OrderByDescending(b => b.Property.PropertyName),
                "createdon" => sortAscending ? query.OrderBy(b => b.CreatedOn) : query.OrderByDescending(b => b.CreatedOn),
                _ => query.OrderBy(b => b.Name)
            };
        }

        private IQueryable<PropertyBeneficiary> ApplyFilter(IQueryable<PropertyBeneficiary> query, string filterField, string filterValue)
        {
            if (string.IsNullOrEmpty(filterValue))
                return query;

            return filterField.ToLower() switch
            {
                "name" => query.Where(b => b.Name.Contains(filterValue)),
                "bentype" => query.Where(b => b.BenType.Name.Contains(filterValue)),
                "status" => query.Where(b => b.BenStatus.Name.Contains(filterValue)),
                "property" => query.Where(b => b.Property.PropertyName.Contains(filterValue) || b.Property.PropertyCode.Contains(filterValue)),
                "agent" => query.Where(b => b.Agent.Contains(filterValue)),
                "ref" => query.Where(b => b.CustomerRefBeneficiary.Contains(filterValue) || b.CustomerRefProperty.Contains(filterValue)),
                _ => query
            };
        }

        private void InvalidateCachedBeneficiaryLists(int companyId, int propertyId)
        {
            var companyListCacheKey = $"{_cacheKeyPrefix}List_Company_{companyId}";
            var propertyListCacheKey = $"{_cacheKeyPrefix}ByProperty_{propertyId}";

            _cache.Remove(companyListCacheKey);
            _cache.Remove(propertyListCacheKey);
        }

        private object GetMonthlyPaymentTrend(ICollection<BeneficiaryPayment> payments)
        {
            // Get payments for the last 12 months
            var startDate = DateTime.Now.AddMonths(-12);

            return payments
                .Where(p => p.CreatedOn >= startDate)
                .GroupBy(p => new { p.CreatedOn.Year, p.CreatedOn.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    TotalPayments = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount),
                    PaidAmount = g.Where(p => p.StatusId == 2).Sum(p => p.Amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();
        }

        #endregion Private Helper Methods
    }
}