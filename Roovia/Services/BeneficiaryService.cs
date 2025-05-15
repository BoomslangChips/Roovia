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
    public class BeneficiaryService : IBeneficiary
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<BeneficiaryService> _logger;
        private readonly IEmailService _emailService;

        public BeneficiaryService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<BeneficiaryService> logger,
            IEmailService emailService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<ResponseModel> CreateBeneficiary(PropertyBeneficiary beneficiary, int companyId)
        {
            ResponseModel response = new();

            try
            {
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

                // Add the beneficiary
                await context.PropertyBeneficiaries.AddAsync(beneficiary);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdBeneficiary = await GetBeneficiaryWithDetails(context, beneficiary.Id);

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
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses)
                    .Include(b => b.ContactNumbers)
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
                    var property = await context.Properties.FindAsync(beneficiary.PropertyId);
                    beneficiary.Amount = property.RentalAmount * (beneficiary.CommissionValue / 100);
                }
                else if (beneficiary.CommissionTypeId == 2) // Fixed amount
                {
                    beneficiary.Amount = beneficiary.CommissionValue;
                }

                beneficiary.PropertyAmount = updatedBeneficiary.PropertyAmount;
                beneficiary.BenStatusId = updatedBeneficiary.BenStatusId;
                beneficiary.NotifyEmail = updatedBeneficiary.NotifyEmail;
                beneficiary.NotifySMS = updatedBeneficiary.NotifySMS;
                beneficiary.CustomerRefBeneficiary = updatedBeneficiary.CustomerRefBeneficiary;
                beneficiary.CustomerRefProperty = updatedBeneficiary.CustomerRefProperty;
                beneficiary.Agent = updatedBeneficiary.Agent;
                beneficiary.Tags = updatedBeneficiary.Tags;
                beneficiary.BankAccount = updatedBeneficiary.BankAccount;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = updatedBeneficiary.UpdatedBy;

                await context.SaveChangesAsync();

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
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.Payments)
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

        public async Task<ResponseModel> GetBeneficiariesByProperty(int propertyId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiaries = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Include(b => b.Payments)
                    .Where(b => b.PropertyId == propertyId && b.CompanyId == companyId && b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                response.Response = beneficiaries;
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

        public async Task<ResponseModel> GetAllBeneficiaries(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiaries = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Property)
                        .ThenInclude(p => p.Owner)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Include(b => b.Payments)
                    .Include(b => b.Company)
                    .Where(b => b.CompanyId == companyId && b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                response.Response = beneficiaries;
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

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary status updated successfully.";

                _logger.LogInformation("Beneficiary status updated: {BeneficiaryId} from {OldStatus} to {NewStatus} by {UserId}",
                    beneficiaryId, oldStatus?.Name, newStatus?.Name, userId);

                // Send notification if status changed to inactive
                if (oldStatusId == 1 && statusId != 1 && beneficiary.NotifyEmail)
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
                        CommissionValue = beneficiary.CommissionValue,
                        CalculatedAmount = Math.Round(amount, 2),
                        BankAccount = beneficiary.BankAccount != null ?
                            $"{beneficiary.BankAccount.BankName} - {beneficiary.BankAccount.AccountNumber}" : "Not specified"
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

        public async Task<ResponseModel> GetBeneficiariesByType(int companyId, int beneficiaryTypeId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiaries = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Property)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Where(b => b.CompanyId == companyId && b.BenTypeId == beneficiaryTypeId && b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                response.Response = beneficiaries;
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
                        PaymentReference = p.PaymentReference,
                        Amount = p.Amount,
                        Status = p.Status?.Name,
                        PaymentDate = p.PaymentDate,
                        TransactionReference = p.TransactionReference,
                        PropertyPayment = p.PaymentAllocation?.Payment?.PaymentReference,
                        PropertyPaymentDate = p.PaymentAllocation?.Payment?.PaymentDate,
                        Notes = p.Notes
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
                        beneficiary.Payments.Where(p => p.StatusId == 2).Average(p => p.Amount) : 0
                };

                response.Response = new
                {
                    Beneficiary = new
                    {
                        Id = beneficiary.Id,
                        Name = beneficiary.Name,
                        Type = beneficiary.BenType?.Name,
                        Status = beneficiary.BenStatus?.Name,
                        CommissionType = beneficiary.CommissionType?.Name,
                        CommissionValue = beneficiary.CommissionValue
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

        public async Task<ResponseModel> SearchBeneficiaries(int companyId, string searchTerm)
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

                var beneficiaries = await query
                    .OrderBy(b => b.Name)
                    .Take(50)
                    .ToListAsync();

                response.Response = beneficiaries;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Found {beneficiaries.Count} beneficiaries matching '{searchTerm}'";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching beneficiaries for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while searching beneficiaries: " + ex.Message;
            }

            return response;
        }
    }
}