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
    public class BeneficiaryService : IBeneficiary
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<BeneficiaryService> _logger;

        public BeneficiaryService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<BeneficiaryService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
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

                // Set audit fields
                beneficiary.CompanyId = companyId;
                beneficiary.CreatedOn = DateTime.Now;

                // Add the beneficiary
                await context.PropertyBeneficiaries.AddAsync(beneficiary);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdBeneficiary = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses)
                    .Include(b => b.ContactNumbers)
                    .Include(b => b.Property)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .FirstOrDefaultAsync(b => b.Id == beneficiary.Id);

                response.Response = createdBeneficiary;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary created successfully.";

                _logger.LogInformation("Beneficiary created with ID: {BeneficiaryId} for property {PropertyId}", beneficiary.Id, beneficiary.PropertyId);
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
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Include(b => b.Payments)
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
                    .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                    return response;
                }

                // Update beneficiary fields
                beneficiary.Name = updatedBeneficiary.Name;
                beneficiary.Address = updatedBeneficiary.Address;
                beneficiary.BenTypeId = updatedBeneficiary.BenTypeId;
                beneficiary.CommissionTypeId = updatedBeneficiary.CommissionTypeId;
                beneficiary.CommissionValue = updatedBeneficiary.CommissionValue;
                beneficiary.Amount = updatedBeneficiary.Amount;
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
                var updatedResult = await context.PropertyBeneficiaries
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Property)
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .FirstOrDefaultAsync(b => b.Id == id);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary updated successfully.";

                _logger.LogInformation("Beneficiary updated: {BeneficiaryId}", id);
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

                // Check if beneficiary has payments
                if (beneficiary.Payments.Any())
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete beneficiary with existing payments.";
                    return response;
                }

                // Soft delete
                beneficiary.IsActive = false;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = user?.Id;

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
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
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
                email.RelatedEntityType = "PropertyBeneficiary";
                email.RelatedEntityId = beneficiaryId;
                email.PropertyBeneficiaryId = beneficiaryId;
                email.CreatedOn = DateTime.Now;
                email.CreatedBy = beneficiary.UpdatedBy;

                beneficiary.EmailAddresses.Add(email);
                await context.SaveChangesAsync();

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address added successfully.";
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
                contactNumber.RelatedEntityType = "PropertyBeneficiary";
                contactNumber.RelatedEntityId = beneficiaryId;
                contactNumber.PropertyBeneficiaryId = beneficiaryId;
                contactNumber.CreatedOn = DateTime.Now;
                contactNumber.CreatedBy = beneficiary.UpdatedBy;

                beneficiary.ContactNumbers.Add(contactNumber);
                await context.SaveChangesAsync();

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number added successfully.";
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

                beneficiary.BenStatusId = statusId;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = userId;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary status updated successfully.";

                _logger.LogInformation("Beneficiary status updated: {BeneficiaryId} to status {StatusId} by {UserId}", beneficiaryId, statusId, userId);
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

                beneficiary.BankAccount = bankAccount;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = userId;

                await context.SaveChangesAsync();

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

        public async Task<ResponseModel> CalculateBeneficiaryAmounts(int propertyId, decimal paymentAmount)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var beneficiaries = await context.PropertyBeneficiaries
                    .Include(b => b.CommissionType)
                    .Where(b => b.PropertyId == propertyId && b.IsActive && b.BenStatusId == 1) // Assuming 1 is Active
                    .ToListAsync();

                var calculations = new List<dynamic>();

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

                    calculations.Add(new
                    {
                        BeneficiaryId = beneficiary.Id,
                        BeneficiaryName = beneficiary.Name,
                        CommissionType = beneficiary.CommissionType?.Name,
                        CommissionValue = beneficiary.CommissionValue,
                        CalculatedAmount = Math.Round(amount, 2)
                    });
                }

                response.Response = calculations;
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
    }
}